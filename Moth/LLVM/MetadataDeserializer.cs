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
    
    public void Process()
    {
        var header = new Reflection.Header();
        _bytes.ReadExactly(new Span<byte>(&header, sizeof(Reflection.Header)));

        throw new NotImplementedException();
    }
}
