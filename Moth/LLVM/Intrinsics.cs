namespace Moth.LLVM.Data;

public sealed class ConstRetFn : IntrinsicFunction
{
    public readonly Value Value;

    private LLVMModuleRef _module;

    public ConstRetFn(string name, LLVMModuleRef module, Value value)
        : base(name,
            new FuncType(value.Type,
                Array.Empty<Type>(),
                LLVMTypeRef.CreateFunction(value.Type.LLVMType,
                    Array.Empty<LLVMTypeRef>())))
    {
        if (!value.LLVMValue.IsConstant)
        {
            throw new ArgumentException("Value needs to be constant.");
        }

        Value = value;
        _module = module;
    }

    public override Value Call(LLVMCompiler compiler, Value[] args) => Value;

    protected override LLVMValueRef GenerateLLVMData()
    {
        LLVMValueRef func = _module.AddFunction(Name, Type.LLVMType);

        using LLVMBuilderRef builder = _module.Context.CreateBuilder();
        builder.PositionAtEnd(func.AppendBasicBlock(""));
        builder.BuildRet(Value.LLVMValue);

        return func;
    }
}

public sealed class Pow : IntrinsicFunction
{
    public Pow(string name, LLVMModuleRef module, Type retType, Type left, Type right)
        : base(name,
            new FuncType(retType,
                new Type[2] { left, right },
                LLVMTypeRef.CreateFunction(retType.LLVMType,
                    new LLVMTypeRef[2] { left.LLVMType, right.LLVMType })))
        => InternalLLVMValue = module.AddFunction(name, Type.LLVMType);

    public override Value Call(LLVMCompiler compiler, Value[] args)
        => new Value(Type.ReturnType, compiler.Builder.BuildCall2(Type.LLVMType, LLVMValue, args.AsLLVMValues(), "pow"));
}