namespace Moth.LLVM.Data;

public class Value : CompilerData
{
    public virtual Type Type { get; }
    public virtual LLVMValueRef LLVMValue { get; }

    public Value(Type type, LLVMValueRef value)
    {
        Type = type;
        LLVMValue = value;
    }
}