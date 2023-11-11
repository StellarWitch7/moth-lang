using Moth.AST.Node;

namespace Moth.LLVM.Data;

public abstract class Function : Value
{
    public readonly string Name;

    protected Function(string name, FuncType type) : base(type, null) => Name = name;

    public new FuncType Type
    {
        get
        {
            return base.Type is FuncType fnType ? fnType : throw new Exception("Function has invalid type.");
        }

        set
        {
            base.Type = value;
        }
    }

    public new LLVMValueRef LLVMValue
    {
        get
        {
            return base.LLVMValue != null
                ? base.LLVMValue
                : throw new Exception("Tried to get null function.");
        }

        set
        {
            base.LLVMValue = value;
        }
    }

    public abstract Value Call(LLVMCompiler compiler, Value[] args);
}

public class LLVMFunction : Function
{
    public Scope? OpeningScope { get; set; }

    public readonly IReadOnlyList<Parameter> Params;
    public readonly bool IsVariadic;

    public LLVMFunction(string name, FuncType type, LLVMValueRef func, IReadOnlyList<Parameter> @params, bool isVariadic)
        : base(name, type)
    {
        LLVMValue = func;
        Params = @params;
        IsVariadic = isVariadic;
    }

    public Class? OwnerClass
    {
        get
        {
            return this is DefinedFunction defFunc
                ? defFunc.OwnerClass
                : null;
        }
    }

    public override Value Call(LLVMCompiler compiler, Value[] args) => Type.Call(compiler, this, args);
}

public sealed class DefinedFunction : LLVMFunction
{
    public new Class? OwnerClass { get; set; }

    public readonly PrivacyType Privacy;

    public DefinedFunction(string name, FuncType type,
        LLVMValueRef func, PrivacyType privacy, Class? ownerClass,
        IReadOnlyList<Parameter> @params, bool isVariadic)
        : base(name, type, func, @params, isVariadic)
    {
        Privacy = privacy;
        OwnerClass = ownerClass;
    }
}

public sealed class LocalFunction : LLVMFunction
{
    public LocalFunction(FuncType type, LLVMValueRef func, IReadOnlyList<Parameter> @params)
        : base("localfunc", type, func, @params, false) { }
}

public abstract class IntrinsicFunction : Function
{
    protected LLVMValueRef InternalLLVMValue;

    protected IntrinsicFunction(string name, FuncType type) : base(name, type) { }

    public new LLVMValueRef LLVMValue
    {
        get
        {
            if (InternalLLVMValue == default)
            {
                InternalLLVMValue = GenerateLLVMData();
            }

            return InternalLLVMValue;
        }
    }

    protected virtual LLVMValueRef GenerateLLVMData()
        => throw new NotImplementedException("This function does not support LLVM data generation.");
}