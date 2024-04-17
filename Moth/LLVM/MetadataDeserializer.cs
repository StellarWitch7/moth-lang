using Moth.LLVM.Data;
using System.Text.RegularExpressions;

namespace Moth.LLVM;

public unsafe class MetadataDeserializer
{
    private LLVMCompiler _compiler;
    private MemoryStream _bytes;
    private Metadata.Version _version = new Metadata.Version();
    private Metadata.Header _header = new Metadata.Header();
    private Metadata.Type[] _types;
    private Metadata.Field[] _fields;
    private Metadata.Function[] _functions;
    private Metadata.Function[] _methods;
    private Metadata.Function[] _staticMethods;
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
        var version = new Metadata.Version();
        _bytes.ReadExactly(new Span<byte>((byte*) &version, sizeof(Metadata.Version)));
        _version = version;

        if (_version.Major != Meta.Version.Major)
        {
            throw new Exception($"Cannot load libary \"{libName}\" due to mismatched major version!" +
                $"\nCompiler: {Meta.Version}" +
                $"\n{libName}: {_version}");
        }

        if (_version.Minor != Meta.Version.Minor)
        {
            _compiler.Warn($"Library \"{libName}\" has mismatched minor version." +
                $"\nCompiler: {Meta.Version}" +
                $"\n{libName}: {_version}");
        }

        var header = new Metadata.Header();
        _bytes.ReadExactly(new Span<byte>(&header, sizeof(Metadata.Header)));
        _header = header;
        
        _types = new Metadata.Type[(int)((_header.field_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Metadata.Type))];
        
        fixed (Metadata.Type* ptr = _types)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Metadata.Type) * _types.Length));
        }

        _fields = new Metadata.Field[(int)((_header.function_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Metadata.Type))];
        
        fixed (Metadata.Field* ptr = _fields)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Metadata.Field) * _fields.Length));
        }
        
        _functions = new Metadata.Function[(int)((_header.method_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Metadata.Function))];
        
        fixed (Metadata.Function* ptr = _functions)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Metadata.Function) * _functions.Length));
        }
        
        //TODO: implement reading of method data
        
        _globals = new Metadata.Global[(int)((_header.functype_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Metadata.Global))];
        
        fixed (Metadata.Global* ptr = _globals)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Metadata.Global) * _globals.Length));
        }
        
        _funcTypes = new Metadata.FuncType[(int)((_header.param_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Metadata.FuncType))];
        
        fixed (Metadata.FuncType* ptr = _funcTypes)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Metadata.FuncType) * _funcTypes.Length));
        }
        
        _parameters = new Metadata.Parameter[(int)((_header.paramtype_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Metadata.Parameter))];
        
        fixed (Metadata.Parameter* ptr = _parameters)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Metadata.Parameter) * _parameters.Length));
        }
        
        _paramTypes = new Metadata.ParamType[(int)((_header.typeref_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Metadata.ParamType))];
        
        fixed (Metadata.ParamType* ptr = _paramTypes)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Metadata.ParamType) * _paramTypes.Length));
        }
        
        _typeRefs = new byte[(int)((_header.name_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(byte))];
        
        fixed (byte* ptr = _typeRefs)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(byte) * _typeRefs.Length));
        }
        
        _names = new byte[(int)((_header.size
                - (ulong)_bytes.Position)
            / (uint)sizeof(byte))];

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
            Struct result;

            if (type.is_foreign)
            {
                result = new OpaqueStruct(_compiler, parent, name, type.privacy);
            }
            else
            {
                result = new Struct(parent,
                    name,
                    _compiler.Context.CreateNamedStruct(fullname),
                    type.privacy);
                result.AddBuiltins(_compiler);
            }
            
            parent.Structs.Add(name, result);
        }

        foreach (var type in _types)
        {
            var name = GetName(type.name_table_index, type.name_table_length, out string fullname);
            var parent = GetNamespace(fullname);
            var result = parent.Structs[name];
            var fields = GetFields(type.field_table_index, type.field_table_length);
            result.LLVMType.StructSetBody(fields.AsLLVMTypes(), false);
        }
        
        throw new NotImplementedException();
    }

    private string GetName(ulong index, ulong length, out string fullname)
    {
        var builder = new StringBuilder();

        for (ulong i = 0; i < length; i++)
        {
            builder.Append((char)_names[index + i]);
        }

        fullname = builder.ToString();
        var match = Regex.Match(fullname, "(?<=(#|\\.))(.*)");

        if (!match.Success)
        {
            return fullname;
        }

        return match.Value;
    }

    private Field[] GetFields(ulong index, ulong length)
    {
        var result = new List<Field>();
        
        for (ulong i = 0; i < length; i++)
        {
            var field = _fields[index + i];
            var name = GetName(field.name_table_index, field.name_table_length, out string fullname);
            var typeref = GetType(field.typeref_table_index, field.typeref_table_length);
            result.Add(new Field(name, (uint)i, typeref, field.privacy));
        }

        return result.ToArray();
    }

    private Type GetType(ulong index, ulong length)
    {
        uint ptrDepth = 0;
        Type result = null;
        
        for (ulong i = 0; i < length; i++)
        {
            switch ((Metadata.TypeTag)_typeRefs[index + i])
            {
                case Metadata.TypeTag.Type:
                    throw new NotImplementedException();
                case Metadata.TypeTag.FuncType:
                    throw new NotImplementedException();
                case Metadata.TypeTag.Pointer:
                    ptrDepth++;
                    break;
                case Metadata.TypeTag.Void:
                    result = Primitives.Void;
                    break;
                case Metadata.TypeTag.Bool:
                    result = Primitives.Bool;
                    break;
                case Metadata.TypeTag.UInt8:
                    result = Primitives.UInt8;
                    break;
                case Metadata.TypeTag.UInt16:
                    result = Primitives.UInt16;
                    break;
                case Metadata.TypeTag.UInt32:
                    result = Primitives.UInt32;
                    break;
                case Metadata.TypeTag.UInt64:
                    result = Primitives.UInt64;
                    break;
                case Metadata.TypeTag.Int16:
                    result = Primitives.Int16;
                    break;
                case Metadata.TypeTag.Int32:
                    result = Primitives.Int32;
                    break;
                case Metadata.TypeTag.Int64:
                    result = Primitives.Int64;
                    break;
                case Metadata.TypeTag.Float16:
                    result = Primitives.Float16;
                    break;
                case Metadata.TypeTag.Float32:
                    result = Primitives.Float32;
                    break;
                case Metadata.TypeTag.Float64:
                    result = Primitives.Float64;
                    break;
                default:
                    throw new NotImplementedException("Type cannot be read.");
            }
        }

        if (result == null)
        {
            throw new Exception("Failed to parse types within metadata, it may be corrupt.");
        }

        for (var i = ptrDepth; i > 0; i--)
        {
            result = new PtrType(result);
        }

        return result;
    }
    
    private Namespace GetNamespace(string name)
    {
        throw new NotImplementedException(); //TODO
        // var match = Regex.Match(name, "(?<=root::)(.*)(?=(#|\\.))");
        //
        // if (!match.Success)
        // {
        //     throw new Exception("Failed to create namespace from metadata, it may be corrupt.");
        // }
        //
        // var cleanName = match.Value;
        // return _compiler.ResolveNamespace(cleanName);
    }
}
