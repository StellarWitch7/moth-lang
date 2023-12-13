using Moth.LLVM.Data;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

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
    
    public void Process()
    {
        var header = new Reflection.Header();
        var size = sizeof(Reflection.Header);
        _bytes.ReadExactly(new Span<byte>(&header, sizeof(Reflection.Header)));
        
        var types = new Reflection.Type[(int)((header.field_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Reflection.Type))];
        
        fixed (Reflection.Type* ptr = types)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Reflection.Type) * types.Length));
        }

        var fields = new Reflection.Field[(int)((header.function_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Reflection.Type))];
        
        fixed (Reflection.Field* ptr = fields)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Reflection.Field) * fields.Length));
        }
        
        var functions = new Reflection.Function[(int)((header.method_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Reflection.Function))];
        
        fixed (Reflection.Function* ptr = functions)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Reflection.Function) * functions.Length));
        }
        
        var globals = new Reflection.Global[(int)((header.functype_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Reflection.Global))];
        
        fixed (Reflection.Global* ptr = globals)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Reflection.Global) * globals.Length));
        }
        
        var funcTypes = new Reflection.FuncType[(int)((header.param_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Reflection.FuncType))];
        
        fixed (Reflection.FuncType* ptr = funcTypes)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Reflection.FuncType) * funcTypes.Length));
        }
        
        var parameters = new Reflection.Parameter[(int)((header.paramtype_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Reflection.Parameter))];
        
        fixed (Reflection.Parameter* ptr = parameters)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Reflection.Parameter) * parameters.Length));
        }
        
        var paramTypes = new Reflection.ParamType[(int)((header.typeref_table_offset
                - (ulong)_bytes.Position)
            / (uint)sizeof(Reflection.ParamType))];
        
        fixed (Reflection.ParamType* ptr = paramTypes)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Reflection.ParamType) * paramTypes.Length));
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
            throw new Exception("Failed to read the entirety of the metadata.");
        }

        throw new NotImplementedException();
    }
}
