namespace Moth.LLVM;

public class Value : CompilerData
{
    public Type Type { get; set; }
    public LLVMValueRef LLVMValue { get; set; }

    public Value(Type type, LLVMValueRef value)
    {
        Type = type;
        LLVMValue = value;
    }
}