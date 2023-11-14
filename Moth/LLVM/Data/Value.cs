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

public class FuncVal : Value
{
    public override FuncType Type { get; }
    
    public FuncVal(FuncType type, LLVMValueRef value) : base(type, value)
    {
        Type = type;

        if (value.Kind != LLVMValueKind.LLVMFunctionValueKind) //TODO: or is it ptr value?
        {
            throw new Exception("Value of a function must be an LLVM function.");
        }
    }

    public virtual Value Call(LLVMCompiler compiler, Value[] args) => Type.Call(compiler, LLVMValue, args);
}

public abstract class IntrinsicFunction : FuncVal
{
    private LLVMValueRef _internalValue;
    
    public IntrinsicFunction(FuncType type) : base(type, null) { }

    public override LLVMValueRef LLVMValue
    {
        get
        {
            if (_internalValue == default)
            {
                _internalValue = GenerateLLVMData();
            }

            return _internalValue;
        }
    }
    
    protected virtual LLVMValueRef GenerateLLVMData()
        => throw new NotImplementedException("This function does not support LLVM data generation.");
}