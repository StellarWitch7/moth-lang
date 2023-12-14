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
            var fields = GetFields(type.field_table_index, type.field_table_length);
            Struct result;
            
            if (type.is_struct)
            {
                result = new Struct(parent,
                    name,
                    LLVMTypeRef.CreateStruct(fields.AsLLVMTypes(),
                        false),
                    type.privacy);
            }
            else
            {
                result = new Class(parent,
                    name,
                    LLVMTypeRef.CreateStruct(fields.AsLLVMTypes(),
                        false),
                    type.privacy);
            }
            
            parent.Structs.Add(name, result);
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
        throw new NotImplementedException(); //TODO
    }
    
    private Namespace GetNamespace(string name)
    {
        var match = Regex.Match(name, "(?<=root::)(.*)(?=(#|\\.))");

        if (!match.Success)
        {
            throw new Exception("Failed to create namespace from metadata, it may be corrupt.");
        }
        
        var cleanName = match.Value;
        return _compiler.ResolveNamespace(cleanName);
    }
}
