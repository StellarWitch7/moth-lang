namespace Moth.LLVM.Data;

public sealed class ConstRetFn : IntrinsicFunction
{
    private LLVMModuleRef _module;
    public readonly LLVMValueRef Value;

    public ConstRetFn(string name, LLVMModuleRef module, ValueContext value) : base(name, value.Type)
    {
        if (!value.LLVMValue.IsConstant)
        {
            throw new ArgumentException("Value needs to be constant.");
        }

        _module = module;
        Value = value.LLVMValue;
    }

    public override LLVMValueRef Call(LLVMBuilderRef builder, ReadOnlySpan<LLVMValueRef> parameters) => Value;

    protected override (LLVMValueRef, LLVMTypeRef) GenerateLLVMData()
    {
        var type = LLVMTypeRef.CreateFunction(Value.TypeOf, new ReadOnlySpan<LLVMTypeRef>(), false);
        LLVMValueRef func = _module.AddFunction(Name, type);

        using LLVMBuilderRef builder = _module.Context.CreateBuilder();
        builder.PositionAtEnd(func.AppendBasicBlock(""));
        builder.BuildRet(Value);

        return (func, type);
    }
}

public sealed class Pow : IntrinsicFunction
{
    public Pow(string name, LLVMModuleRef module, Type ret, LLVMTypeRef p1, LLVMTypeRef p2) : base(name, ret)
    {
        InternalLLVMFuncType = LLVMTypeRef.CreateFunction(ret.LLVMType, stackalloc LLVMTypeRef[] { p1, p2 }, false);
        InternalLLVMFunc = module.AddFunction(name, InternalLLVMFuncType);
    }

    public override LLVMValueRef Call(LLVMBuilderRef builder, ReadOnlySpan<LLVMValueRef> parameters) => builder.BuildCall2(LLVMFuncType, LLVMFunc, parameters, "pow");
}