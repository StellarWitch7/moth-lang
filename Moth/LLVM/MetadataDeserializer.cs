using Moth.LLVM.Data;

namespace Moth.LLVM;

public unsafe class MetadataDeserializer
{
    private LLVMCompiler _compiler;
    private MemoryStream _bytes;
    
    public MetadataDeserializer(LLVMCompiler compiler, MemoryStream bytes)
    {
        _compiler = compiler;
        _bytes = bytes;
    }
    
    public void Process(string libName)
    {
        var version = new Metadata.Version();
        _bytes.ReadExactly(new Span<byte>((byte*) &version, sizeof(Metadata.Version)));

        if (version.Major != Meta.Version.Major)
        {
            throw new Exception($"Cannot load libary \"{libName}\" due to mismatched major version!" +
                $"\nCompiler: {Meta.Version}" +
                $"\n{libName}: {version}");
        }

        if (version.Minor != Meta.Version.Minor)
        {
            _compiler.Warn($"Library \"{libName}\" has mismatched minor version." +
                $"\nCompiler: {Meta.Version}" +
                $"\n{libName}: {version}");
        }
        
        var header = new Metadata.Header();
        var size = sizeof(Metadata.Header);
        _bytes.ReadExactly(new Span<byte>(&header, sizeof(Metadata.Header)));
        
        var types = new Metadata.Type[(int)((header.field_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Metadata.Type))];
        
        fixed (Metadata.Type* ptr = types)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Metadata.Type) * types.Length));
        }

        var fields = new Metadata.Field[(int)((header.function_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Metadata.Type))];
        
        fixed (Metadata.Field* ptr = fields)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Metadata.Field) * fields.Length));
        }
        
        var functions = new Metadata.Function[(int)((header.method_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Metadata.Function))];
        
        fixed (Metadata.Function* ptr = functions)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Metadata.Function) * functions.Length));
        }
        
        var globals = new Metadata.Global[(int)((header.functype_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Metadata.Global))];
        
        fixed (Metadata.Global* ptr = globals)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Metadata.Global) * globals.Length));
        }
        
        var funcTypes = new Metadata.FuncType[(int)((header.param_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Metadata.FuncType))];
        
        fixed (Metadata.FuncType* ptr = funcTypes)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Metadata.FuncType) * funcTypes.Length));
        }
        
        var parameters = new Metadata.Parameter[(int)((header.paramtype_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Metadata.Parameter))];
        
        fixed (Metadata.Parameter* ptr = parameters)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Metadata.Parameter) * parameters.Length));
        }
        
        var paramTypes = new Metadata.ParamType[(int)((header.typeref_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Metadata.ParamType))];
        
        fixed (Metadata.ParamType* ptr = paramTypes)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Metadata.ParamType) * paramTypes.Length));
        }
        
        var typeRefs = new byte[(int)((header.name_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(byte))];
        
        fixed (byte* ptr = typeRefs)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(byte) * typeRefs.Length));
        }
        
        var names = new byte[(int)((header.size
                - (ulong)_bytes.Position)
            / (uint)sizeof(byte))];

        fixed (byte* ptr = names)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(byte) * names.Length));
        }

        if (_bytes.ReadByte() != -1)
        {
            throw new Exception($"Failed to read the entirety of the metadata for \"{libName}\".");
        }

        throw new NotImplementedException();
    }
}
