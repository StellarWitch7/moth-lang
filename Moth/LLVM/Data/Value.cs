using Moth.AST.Node;

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

public class Function : Value
{
    public override FuncType Type { get; }
    public Parameter[] Params { get; }
    public Scope? OpeningScope { get; set; }
    
    public Function(FuncType type, LLVMValueRef value, Parameter[] @params) : base(type, value)
    {
        Type = type;
        Params = @params;

        if (value.Kind != LLVMValueKind.LLVMFunctionValueKind) //TODO: or is it ptr value?
        {
            throw new Exception("Value of a function must be an LLVM function.");
        }
    }

    public virtual Value Call(LLVMCompiler compiler, Value[] args) => Type.Call(compiler, LLVMValue, args);
}

public class DefinedFunction : Function
{
    public PrivacyType Privacy { get; }
    
    public DefinedFunction(FuncType type, LLVMValueRef value, Parameter[] @params, PrivacyType privacy)
        : base(type, value, @params)
    {
        Privacy = privacy;
    }
}

public abstract class IntrinsicFunction : Function
{
    private LLVMValueRef _internalValue;
    
    public IntrinsicFunction(FuncType type) : base(type, default, new Parameter[0]) { }

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