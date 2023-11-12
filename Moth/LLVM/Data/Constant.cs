namespace Moth.LLVM.Data;

public class Constant : CompilerData
{
    public ClassType Type { get; set; }
    public LLVMValueRef LLVMValue { get; set; }

    public Constant(ClassType type, LLVMValueRef value)
    {
        Type = type;
        LLVMValue = value;
    }
}
