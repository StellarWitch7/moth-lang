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
        
        var types = new Type[(int)(header.field_table_offset - header.type_table_offset)];
        
        fixed (Type* ptr = types)
        {
            _bytes.ReadExactly(new Span<byte>((byte*)ptr, sizeof(Type) * types.Length));
        }

        throw new NotImplementedException();
    }
}
