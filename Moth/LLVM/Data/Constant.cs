using LLVMSharp.Interop;

namespace Moth.LLVM.Data;

public class Constant
{
    public Type Type { get; set; }
    public LLVMValueRef LLVMValue { get; set; }

    public Constant(Type type, LLVMValueRef value)
    {
        Type = type;
        LLVMValue = value;
    }
}
