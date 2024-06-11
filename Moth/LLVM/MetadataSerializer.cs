using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using LLVMSharp;
using Moth.LLVM.Data;

namespace Moth.LLVM;

public unsafe class MetadataSerializer
{
    private MemoryStream _stream = new MemoryStream();
    private List<Metadata.Type> _types = new List<Metadata.Type>();
    private List<Metadata.Field> _fields = new List<Metadata.Field>();
    private List<Metadata.Function> _functions = new List<Metadata.Function>();
    private List<Metadata.Global> _globals = new List<Metadata.Global>();
    private List<Metadata.FuncType> _funcTypes = new List<Metadata.FuncType>();
    private List<Metadata.Parameter> _params = new List<Metadata.Parameter>();
    private List<Metadata.ParamType> _paramTypes = new List<Metadata.ParamType>();
    private List<byte> _typeRefs = new List<byte>();
    private List<string> _names = new List<string>();
    private Dictionary<Data.Type, uint> _typeIndexes = new Dictionary<Data.Type, uint>();
    private Dictionary<Data.FuncType, uint> _functypeIndexes =
        new Dictionary<Data.FuncType, uint>();
    private uint _typeTablePosition = 0;
    private uint _fieldTablePosition = 0;
    private uint _functionTablePosition = 0;
    private uint _globalTablePosition = 0;
    private uint _functypeTablePosition = 0;
    private uint _paramTablePosition = 0;
    private uint _paramTypeTablePosition = 0;
    private uint _nameTablePosition = 0;
    private uint _typeRefTablePosition = 0;
    private LLVMCompiler _compiler;

    public MetadataSerializer(LLVMCompiler compiler)
    {
        _compiler = compiler;
    }

    public MetadataSerializer(LLVMCompiler compiler, MemoryStream stream)
        : this(compiler)
    {
        _stream = stream;
    }

    public MemoryStream Process()
    {
        var startPos = (uint)_stream.Position;

        foreach (var typeDecl in _compiler.Types)
        {
            var newType = new Metadata.Type();
            newType.privacy = typeDecl.Privacy;
            newType.is_foreign = typeDecl is OpaqueStructDecl;
            newType.is_union = typeDecl.IsUnion;
            newType.name_table_index = _nameTablePosition;
            newType.name_table_length = (uint)typeDecl.FullName.Length;
            AddName(typeDecl.FullName);

            if (typeDecl is StructDecl structDecl)
            {
                newType.field_table_index = _fieldTablePosition;
                newType.field_table_length = (uint)structDecl.Fields.Count;

                foreach (var field in structDecl.Fields.Values)
                {
                    var newField = new Metadata.Field();
                    (uint typerefIndex, uint typerefLength) = AddTypeRef(field.Type);
                    newField.privacy = field.Privacy;
                    newField.typeref_table_index = typerefIndex;
                    newField.typeref_table_length = typerefLength;
                    newField.name_table_index = _nameTablePosition;
                    newField.name_table_length = (uint)field.Name.Length;
                    AddName(field.Name);
                    AddField(newField);
                }
            }

            AddType(typeDecl, newType);
        }

        foreach (var func in _compiler.Functions)
        {
            var newFunc = new Metadata.Function();
            (uint typerefIndex, uint typerefLength) = AddTypeRef(func.Type);
            newFunc.is_method = !func.IsStatic;
            newFunc.privacy = func.Privacy;
            newFunc.typeref_table_index = typerefIndex;
            newFunc.typeref_table_length = typerefLength;
            newFunc.name_table_index = _nameTablePosition;
            newFunc.name_table_length = (uint)func.FullName.Length;
            AddName(func.FullName);

            foreach (var param in func.Params)
            {
                var newParam = new Metadata.Parameter();
                newParam.name_table_index = _nameTablePosition;
                newParam.name_table_length = (uint)param.Name.Length;
                newParam.param_index = param.ParamIndex;
                AddName(param.Name);
                AddParam(newParam);
            }

            AddFunction(newFunc);
        }

        foreach (var global in _compiler.Globals)
        {
            var newGlobal = new Metadata.Global();
            (uint typerefIndex, uint typerefLength) = AddTypeRef(global.Type.BaseType);
            newGlobal.privacy = global.Privacy;
            newGlobal.is_constant = global is GlobalConstant;
            newGlobal.typeref_table_index = typerefIndex;
            newGlobal.typeref_table_length = typerefLength;
            newGlobal.name_table_index = _nameTablePosition;
            newGlobal.name_table_length = (uint)global.FullName.Length;
            AddName(global.FullName);
            AddGlobal(newGlobal);
        }

        // write the results
        // metadata version first
        var version = Meta.Version;
        _stream.Write(new ReadOnlySpan<byte>((byte*)&version, sizeof(Version)));
        // then module version
        var moduleVersion = _compiler.ModuleVersion;
        _stream.Write(new ReadOnlySpan<byte>((byte*)&moduleVersion, sizeof(Version)));
        // and then the metadata header
        Metadata.Header header = new Metadata.Header();
        _stream.Write(new ReadOnlySpan<byte>((byte*)&header, sizeof(Metadata.Header)));

        header.type_table_offset = (uint)_stream.Position - startPos;

        fixed (Metadata.Type* ptr = CollectionsMarshal.AsSpan(_types))
        {
            _stream.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Metadata.Type) * _types.Count));
        }

        header.field_table_offset = (uint)_stream.Position - startPos;

        fixed (Metadata.Field* ptr = CollectionsMarshal.AsSpan(_fields))
        {
            _stream.Write(
                new ReadOnlySpan<byte>((byte*)ptr, sizeof(Metadata.Field) * _fields.Count)
            );
        }

        header.function_table_offset = (uint)_stream.Position - startPos;

        fixed (Metadata.Function* ptr = CollectionsMarshal.AsSpan(_functions))
        {
            _stream.Write(
                new ReadOnlySpan<byte>((byte*)ptr, sizeof(Metadata.Function) * _functions.Count)
            );
        }

        header.global_variable_table_offset = (uint)_stream.Position - startPos;

        fixed (Metadata.Global* ptr = CollectionsMarshal.AsSpan(_globals))
        {
            _stream.Write(
                new ReadOnlySpan<byte>((byte*)ptr, sizeof(Metadata.Global) * _globals.Count)
            );
        }

        header.functype_table_offset = (uint)_stream.Position - startPos;

        fixed (Metadata.FuncType* ptr = CollectionsMarshal.AsSpan(_funcTypes))
        {
            _stream.Write(
                new ReadOnlySpan<byte>((byte*)ptr, sizeof(Metadata.FuncType) * _funcTypes.Count)
            );
        }

        header.param_table_offset = (uint)_stream.Position - startPos;

        fixed (Metadata.Parameter* ptr = CollectionsMarshal.AsSpan(_params))
        {
            _stream.Write(
                new ReadOnlySpan<byte>((byte*)ptr, sizeof(Metadata.Parameter) * _params.Count)
            );
        }

        header.paramtype_table_offset = (uint)_stream.Position - startPos;

        fixed (Metadata.ParamType* ptr = CollectionsMarshal.AsSpan(_paramTypes))
        {
            _stream.Write(
                new ReadOnlySpan<byte>((byte*)ptr, sizeof(Metadata.ParamType) * _paramTypes.Count)
            );
        }

        header.typeref_table_offset = (uint)_stream.Position - startPos;

        fixed (byte* ptr = CollectionsMarshal.AsSpan(_typeRefs))
        {
            _stream.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(byte) * _typeRefs.Count));
        }

        header.name_table_offset = (uint)_stream.Position - startPos;

        foreach (var name in _names)
        {
            _stream.Write(System.Text.Encoding.UTF8.GetBytes(name));
        }

        header.size = (uint)_stream.Position - startPos;

        _stream.Seek((long)startPos, SeekOrigin.Begin);
        _stream.Write(new ReadOnlySpan<byte>((byte*)&version, sizeof(Version)));
        _stream.Write(new ReadOnlySpan<byte>((byte*)&moduleVersion, sizeof(Version)));
        _stream.Write(new ReadOnlySpan<byte>((byte*)&header, sizeof(Metadata.Header)));
        _stream.Seek(0, SeekOrigin.End);
        return _stream;
    }

    public void AddType(Data.TypeDecl typeDecl, Metadata.Type metaType)
    {
        _typeIndexes.Add(typeDecl, _typeTablePosition);
        _types.Add(metaType);
        _typeTablePosition++;
    }

    public uint AddFuncType(Data.FuncType originalType, Metadata.FuncType type)
    {
        var pos = _functypeTablePosition;
        _functypeIndexes.Add(originalType, _functypeTablePosition);
        _funcTypes.Add(type);
        _functypeTablePosition++;
        return pos;
    }

    public void AddField(Metadata.Field field)
    {
        _fields.Add(field);
        _fieldTablePosition++;
    }

    public void AddFunction(Metadata.Function func)
    {
        _functions.Add(func);
        _functionTablePosition++;
    }

    public void AddGlobal(Metadata.Global global)
    {
        _globals.Add(global);
        _globalTablePosition++;
    }

    public void AddParam(Metadata.Parameter param)
    {
        _params.Add(param);
        _paramTablePosition++;
    }

    public void AddParamType(Metadata.ParamType paramType)
    {
        _paramTypes.Add(paramType);
        _paramTypeTablePosition++;
    }

    public void AddName(string name)
    {
        _names.Add(name);
        _nameTablePosition += (uint)name.Length;
    }

    public (uint, uint) AddTypeRef(Type type)
    {
        var result = new List<byte>();

        while (type != null)
        {
            if (type is RefType refType && type.GetType() == typeof(RefType))
            {
                result.Add((byte)Metadata.TypeTag.Reference);
                type = refType.BaseType;
            }
            if (type is PtrType ptrType && type.GetType() == typeof(PtrType))
            {
                result.Add((byte)Metadata.TypeTag.Pointer);
                type = ptrType.BaseType;
            }
            else if (type == _compiler.Void)
            {
                result.Add((byte)Metadata.TypeTag.Void);
                type = null;
            }
            else if (type == _compiler.Bool)
            {
                result.Add((byte)Metadata.TypeTag.Bool);
                type = null;
            }
            else if (type == _compiler.UInt8)
            {
                result.Add((byte)Metadata.TypeTag.UInt8);
                type = null;
            }
            else if (type == _compiler.UInt16)
            {
                result.Add((byte)Metadata.TypeTag.UInt16);
                type = null;
            }
            else if (type == _compiler.UInt32)
            {
                result.Add((byte)Metadata.TypeTag.UInt32);
                type = null;
            }
            else if (type == _compiler.UInt64)
            {
                result.Add((byte)Metadata.TypeTag.UInt64);
                type = null;
            }
            else if (type == _compiler.Int8)
            {
                result.Add((byte)Metadata.TypeTag.Int8);
                type = null;
            }
            else if (type == _compiler.Int16)
            {
                result.Add((byte)Metadata.TypeTag.Int16);
                type = null;
            }
            else if (type == _compiler.Int32)
            {
                result.Add((byte)Metadata.TypeTag.Int32);
                type = null;
            }
            else if (type == _compiler.Int64)
            {
                result.Add((byte)Metadata.TypeTag.Int64);
                type = null;
            }
            else
            {
                if (_typeIndexes.TryGetValue(type, out uint index))
                {
                    result.Add((byte)Metadata.TypeTag.Type);
                    result.AddRange(new ReadOnlySpan<byte>((byte*)&index, sizeof(uint)).ToArray());
                    type = null;
                }
                else
                {
                    if (type is Data.FuncType fnType)
                    {
                        result.Add((byte)Metadata.TypeTag.FuncType);

                        if (!_functypeIndexes.TryGetValue(fnType, out index))
                        {
                            var newFuncType = new Metadata.FuncType();
                            (uint retTyperefIndex, uint retTyperefLength) = AddTypeRef(
                                fnType.ReturnType
                            );

                            newFuncType.is_variadic = fnType.IsVariadic;
                            newFuncType.return_typeref_table_index = retTyperefIndex;
                            newFuncType.return_typeref_table_length = retTyperefLength;
                            newFuncType.paramtype_table_index = _paramTypeTablePosition;
                            newFuncType.paramtype_table_length = (uint)fnType.ParameterTypes.Length;

                            foreach (var paramType in fnType.ParameterTypes)
                            {
                                var newParamType = new Metadata.ParamType();
                                (uint typerefIndex, uint typerefLength) = AddTypeRef(paramType);
                                newParamType.typeref_table_index = typerefIndex;
                                newParamType.typeref_table_length = typerefLength;
                                AddParamType(newParamType);
                            }

                            index = AddFuncType(fnType, newFuncType);
                        }

                        result.AddRange(
                            new ReadOnlySpan<byte>((byte*)&index, sizeof(uint)).ToArray()
                        );
                        type = null;
                    }
                    else
                    {
                        throw new Exception(
                            $"Cannot retrieve non-indexed type, and cannot create it. "
                                + $"This is a CRITICAL ERROR. Report ASAP."
                        );
                    }
                }
            }
        }

        var tableIndex = _typeRefTablePosition;
        _typeRefs.AddRange(result);
        _typeRefTablePosition += (uint)result.Count;
        return (tableIndex, (uint)result.Count);
    }
}
