using System.Text.RegularExpressions;
using Moth.AST.Node;
using Moth.LLVM.Data;

namespace Moth.LLVM; //TODO: note that all instances of "new Dictionary<string, IAttribute>()" probably need to be replaced

//TODO: unless they don't? maybe it is better if attributes aren't kept in metadata

public unsafe class MetadataDeserializer
{
    private LLVMCompiler _compiler;
    private MemoryStream _bytes;
    private Version _version = new Version();
    private Version _moduleVersion = new Version();
    private Metadata.Header _header = new Metadata.Header();
    private Metadata.Type[] _types;
    private Metadata.Field[] _fields;
    private Metadata.Function[] _functions;
    private Metadata.Global[] _globals;
    private Metadata.FuncType[] _funcTypes;
    private Metadata.Parameter[] _parameters;
    private Metadata.ParamType[] _paramTypes;
    private byte[] _typeRefs;
    private byte[] _names;

    public MetadataDeserializer(LLVMCompiler compiler, MemoryStream bytes)
    {
        _compiler = compiler;
        _bytes = bytes;
    }

    public void Process(string libName)
    {
        var version = new Version();
        _bytes.ReadExactly(new Span<byte>((byte*)&version, sizeof(Version)));
        _version = version;

        if (_version.Major != Meta.Version.Major)
        {
            throw new Exception(
                $"Cannot load libary \"{libName}\" due to mismatched major version!"
                    + $"\nCompiler: {Meta.Version}"
                    + $"\n{libName}: {_version}"
            );
        }

        if (_version.Minor != Meta.Version.Minor)
        {
            _compiler.Warn(
                $"Library \"{libName}\" has mismatched minor version."
                    + $"\nCompiler: {Meta.Version}"
                    + $"\n{libName}: {_version}"
            );
        }

        if (_version.Patch != Meta.Version.Patch)
        {
            _compiler.Warn(
                $"Library \"{libName}\" has mismatched patch version."
                    + $"\nCompiler: {Meta.Version}"
                    + $"\n{libName}: {_version}"
            );
        }

        var moduleVersion = new Version();
        _bytes.ReadExactly(new Span<byte>(&moduleVersion, sizeof(Version)));
        _moduleVersion = moduleVersion;

        var header = new Metadata.Header();
        _bytes.ReadExactly(new Span<byte>(&header, sizeof(Metadata.Header)));
        _header = header;

        _types = new Metadata.Type[
            (_header.field_table_offset - (uint)_bytes.Position) / (uint)sizeof(Metadata.Type)
        ];

        fixed (Metadata.Type* ptr = _types)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Metadata.Type) * _types.Length));
        }

        _fields = new Metadata.Field[
            (_header.function_table_offset - (uint)_bytes.Position) / (uint)sizeof(Metadata.Type)
        ];

        fixed (Metadata.Field* ptr = _fields)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Metadata.Field) * _fields.Length));
        }

        _functions = new Metadata.Function[
            (_header.global_variable_table_offset - (uint)_bytes.Position)
                / (uint)sizeof(Metadata.Function)
        ];

        fixed (Metadata.Function* ptr = _functions)
        {
            _bytes.ReadExactly(
                new Span<byte>((byte*)ptr, sizeof(Metadata.Function) * _functions.Length)
            );
        }

        _globals = new Metadata.Global[
            (_header.functype_table_offset - (uint)_bytes.Position) / (uint)sizeof(Metadata.Global)
        ];

        fixed (Metadata.Global* ptr = _globals)
        {
            _bytes.ReadExactly(
                new Span<byte>((byte*)ptr, sizeof(Metadata.Global) * _globals.Length)
            );
        }

        _funcTypes = new Metadata.FuncType[
            (_header.param_table_offset - (uint)_bytes.Position) / (uint)sizeof(Metadata.FuncType)
        ];

        fixed (Metadata.FuncType* ptr = _funcTypes)
        {
            _bytes.ReadExactly(
                new Span<byte>((byte*)ptr, sizeof(Metadata.FuncType) * _funcTypes.Length)
            );
        }

        _parameters = new Metadata.Parameter[
            (_header.paramtype_table_offset - (uint)_bytes.Position)
                / (uint)sizeof(Metadata.Parameter)
        ];

        fixed (Metadata.Parameter* ptr = _parameters)
        {
            _bytes.ReadExactly(
                new Span<byte>((byte*)ptr, sizeof(Metadata.Parameter) * _parameters.Length)
            );
        }

        _paramTypes = new Metadata.ParamType[
            (_header.typeref_table_offset - (uint)_bytes.Position)
                / (uint)sizeof(Metadata.ParamType)
        ];

        fixed (Metadata.ParamType* ptr = _paramTypes)
        {
            _bytes.ReadExactly(
                new Span<byte>((byte*)ptr, sizeof(Metadata.ParamType) * _paramTypes.Length)
            );
        }

        _typeRefs = new byte[
            (_header.name_table_offset - (uint)_bytes.Position) / (uint)sizeof(byte)
        ];

        fixed (byte* ptr = _typeRefs)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(byte) * _typeRefs.Length));
        }

        _names = new byte[(_header.size - (uint)_bytes.Position) / (uint)sizeof(byte)];

        fixed (byte* ptr = _names)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(byte) * _names.Length));
        }

        if (_bytes.ReadByte() != -1)
        {
            throw new Exception($"Failed to read the entirety of the metadata for \"{libName}\".");
        }

        foreach (var type in _types)
        {
            var name = GetName(type.name_table_index, type.name_table_length, out string fullname);
            var parent = GetNamespace(fullname);
            StructDecl result;

            if (type.is_foreign)
            {
                result = new OpaqueStructDecl(
                    _compiler,
                    parent,
                    name,
                    type.privacy,
                    type.is_union,
                    new Dictionary<string, IAttribute>()
                )
                {
                    IsExternal = true
                };
            }
            else
            {
                result = new StructDecl(
                    _compiler,
                    parent,
                    name,
                    type.privacy,
                    type.is_union,
                    new Dictionary<string, IAttribute>(),
                    _compiler.Context.CreateNamedStruct(fullname)
                )
                {
                    IsExternal = true
                };
                result.AddBuiltins();
            }

            parent.Types.Add(name, result);
        }

        foreach (var type in _types)
        {
            var name = GetName(type.name_table_index, type.name_table_length, out string fullname);
            var parent = GetNamespace(fullname);
            var result = parent.Types[name];

            if (result is StructDecl structDecl && structDecl is not OpaqueStructDecl) //TODO: do other type decls need to be handled?
            {
                var fields = GetFields(structDecl, type.field_table_index, type.field_table_length);
                result.LLVMType.StructSetBody(fields.AsLLVMTypes(), false);
            }
        }

        foreach (var func in _functions)
        {
            var name = TrimSigFromName(
                GetName(func.name_table_index, func.name_table_length, out string fullname)
            );
            var parentNmspace = GetNamespace(fullname);
            var overloadList = new OverloadList(name);
            IContainer parent;

            if (TryGetTypeDeclByString(fullname, out TypeDecl typeDecl))
            {
                if (func.is_method)
                {
                    typeDecl.Methods.TryAdd(name, overloadList);
                    overloadList = typeDecl.Methods[name];
                }
                else
                {
                    typeDecl.StaticMethods.TryAdd(name, overloadList);
                    overloadList = typeDecl.StaticMethods[name];
                }

                parent = typeDecl;
            }
            else
            {
                parentNmspace.Functions.TryAdd(name, overloadList);
                overloadList = parentNmspace.Functions[name];
                parent = parentNmspace;
            }

            var funcType = GetType(func.typeref_table_index, func.typeref_table_length)
                is FuncType fnType
                ? fnType
                : throw new Exception(
                    "Internal error: function type in metadata is not a valid function type."
                );
            Function result = new DefinedFunction(
                _compiler,
                parent,
                fullname,
                funcType,
                null,
                func.privacy,
                true,
                new Dictionary<string, IAttribute>()
            )
            {
                IsExternal = true
            };
            overloadList.Add(result);
        }

        foreach (var global in _globals)
        {
            var name = GetName(
                global.name_table_index,
                global.name_table_length,
                out string fullname
            );
            var nmspace = GetNamespace(fullname);
            var type = GetType(global.typeref_table_index, global.typeref_table_length);
            IGlobal result = global.is_constant
                ? new GlobalConstant(
                    _compiler,
                    nmspace,
                    name,
                    new VarType(_compiler, type),
                    _compiler.Module.AddGlobal(type.LLVMType, fullname),
                    new Dictionary<string, IAttribute>(),
                    global.privacy
                )
                {
                    IsExternal = true
                }
                : new GlobalVariable(
                    _compiler,
                    nmspace,
                    name,
                    new VarType(_compiler, type),
                    _compiler.Module.AddGlobal(type.LLVMType, fullname),
                    new Dictionary<string, IAttribute>(),
                    global.privacy
                )
                {
                    IsExternal = true
                };
            nmspace.GlobalVariables.Add(name, result);
        }
    }

    private bool TryGetTypeDeclByString(string fullname, out TypeDecl typeDecl)
    {
        var match = Regex.Match(fullname, "#(.*)\\.");

        if (!match.Success)
        {
            typeDecl = null;
            return false;
        }

        var nmspace = GetNamespace(fullname);
        typeDecl = nmspace.Types[match.Groups[1].Value];
        return true;
    }

    private string TrimSigFromName(string name)
    {
        var match = Regex.Match(name, "[^\\(]+");

        if (!match.Success)
            throw new Exception("");

        return match.Value;
    }

    private string GetName(uint index, uint length, out string fullname)
    {
        var builder = new StringBuilder();

        for (uint i = 0; i < length; i++)
        {
            builder.Append((char)_names[index + i]);
        }

        fullname = builder.ToString();
        var match = Regex.Match(fullname, "((?<=\\.).*$|(?<=#)[^\\.]+$)");

        if (!match.Success)
        {
            return fullname;
        }

        return match.Value;
    }

    private Field[] GetFields(StructDecl structDecl, uint index, uint length)
    {
        var result = new List<Field>();

        for (uint i = 0; i < length; i++)
        {
            var field = _fields[index + i];
            var name = GetName(
                field.name_table_index,
                field.name_table_length,
                out string fullname
            );
            var typeref = GetType(field.typeref_table_index, field.typeref_table_length);
            result.Add(new Field(_compiler, structDecl, name, (uint)i, typeref, field.privacy));
        }

        return result.ToArray();
    }

    private Data.Type GetType(uint index, uint length)
    {
        var ptrOrRef = new List<bool>();
        Data.Type result = null;

        for (uint i = 0; i < length; i++)
        {
            switch ((Metadata.TypeTag)_typeRefs[index + i])
            {
                case Metadata.TypeTag.Type:
                {
                    var bytes = new MemoryStream(
                        _typeRefs,
                        (int)(index + i + 1),
                        sizeof(uint),
                        false
                    );
                    uint typeIndex;

                    bytes.ReadExactly(new Span<byte>((byte*)&typeIndex, sizeof(uint)));
                    i += sizeof(uint);

                    var type = _types[typeIndex];
                    var name = GetName(
                        type.name_table_index,
                        type.name_table_length,
                        out string fullname
                    );
                    var nmspace = GetNamespace(fullname);
                    StructDecl newStructDecl;

                    if (type.is_foreign)
                    {
                        newStructDecl = new OpaqueStructDecl(
                            _compiler,
                            nmspace,
                            name,
                            type.privacy,
                            type.is_union,
                            new Dictionary<string, IAttribute>()
                        );
                    }
                    else
                    {
                        newStructDecl = new StructDecl(
                            _compiler,
                            nmspace,
                            name,
                            type.privacy,
                            type.is_union,
                            new Dictionary<string, IAttribute>(),
                            (
                                decl =>
                                    LLVMTypeRef.CreateStruct(
                                        GetFields(
                                                decl as StructDecl,
                                                type.field_table_index,
                                                type.field_table_length
                                            )
                                            .AsLLVMTypes(),
                                        false
                                    )
                            )
                        )
                        {
                            OriginModuleVersion = _moduleVersion
                        };
                    }

                    nmspace.Types.TryAdd(name, newStructDecl);
                    result = newStructDecl;
                    break;
                }
                case Metadata.TypeTag.FuncType:
                {
                    var bytes = new MemoryStream(
                        _typeRefs,
                        (int)(index + i + 1),
                        sizeof(uint),
                        false
                    );
                    uint typeIndex;

                    bytes.ReadExactly(new Span<byte>((byte*)&typeIndex, sizeof(uint)));
                    i += sizeof(uint);

                    var type = _funcTypes[typeIndex];
                    var retType = GetType(
                        type.return_typeref_table_index,
                        type.return_typeref_table_length
                    );
                    var paramTypes = GetParamTypes(
                        type.paramtype_table_index,
                        type.paramtype_table_length
                    );

                    result = new FuncType(_compiler, retType, paramTypes, type.is_variadic);
                    break;
                }
                case Metadata.TypeTag.Pointer:
                    ptrOrRef.Add(false);
                    break;
                case Metadata.TypeTag.Reference:
                    ptrOrRef.Add(true);
                    break;
                case Metadata.TypeTag.Void:
                    result = _compiler.Void;
                    break;
                case Metadata.TypeTag.Bool:
                    result = _compiler.Bool;
                    break;
                case Metadata.TypeTag.UInt8:
                    result = _compiler.UInt8;
                    break;
                case Metadata.TypeTag.UInt16:
                    result = _compiler.UInt16;
                    break;
                case Metadata.TypeTag.UInt32:
                    result = _compiler.UInt32;
                    break;
                case Metadata.TypeTag.UInt64:
                    result = _compiler.UInt64;
                    break;
                case Metadata.TypeTag.Int16:
                    result = _compiler.Int16;
                    break;
                case Metadata.TypeTag.Int32:
                    result = _compiler.Int32;
                    break;
                case Metadata.TypeTag.Int64:
                    result = _compiler.Int64;
                    break;
                case Metadata.TypeTag.Float16:
                    result = _compiler.Float16;
                    break;
                case Metadata.TypeTag.Float32:
                    result = _compiler.Float32;
                    break;
                case Metadata.TypeTag.Float64:
                    result = _compiler.Float64;
                    break;
                default:
                    throw new NotImplementedException("Type cannot be read.");
            }
        }

        if (result == null)
        {
            throw new Exception("Failed to parse types within metadata, it may be corrupt.");
        }

        foreach (var b in ptrOrRef)
        {
            result = b ? new RefType(_compiler, result) : result = new PtrType(_compiler, result);
        }

        return result;
    }

    private Data.Type[] GetParamTypes(uint index, uint length)
    {
        var types = new Data.Type[length];

        for (uint i = 0; i < length; i++)
        {
            var paramType = _paramTypes[index + i];
            types[i] = GetType(paramType.typeref_table_index, paramType.typeref_table_length);
        }

        return types;
    }

    private Namespace GetNamespace(string fullname)
    {
        var match = Regex.Match(fullname, "(?<=root::)[a-zA-Z_:]+");

        if (!match.Success)
            throw new Exception("Failed to get namespace from metadata, it may be corrupt.");

        var cleanName = match.Value;
        NamespaceNode nmspace = null;
        NamespaceNode lastNmspace = null;

        foreach (var str in cleanName.Split("::"))
        {
            if (nmspace == null)
            {
                nmspace = new NamespaceNode(str);
                lastNmspace = nmspace;
            }
            else
            {
                lastNmspace.Child = new NamespaceNode(str);
                lastNmspace = lastNmspace.Child;
            }
        }

        return _compiler.ResolveNamespace(nmspace);
    }
}
