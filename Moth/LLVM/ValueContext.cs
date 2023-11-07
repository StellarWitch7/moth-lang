using LLVMSharp.Interop;

namespace Moth.LLVM;

public class ValueContext
{
    public Type Type { get; set; }
    public LLVMValueRef LLVMValue { get; set; }

    public ValueContext(Type type, LLVMValueRef value)
    {
        Type = type;
        LLVMValue = value;
    }
}
