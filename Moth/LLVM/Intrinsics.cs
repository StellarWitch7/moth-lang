namespace Moth.LLVM.Data;

public sealed class ConstRetFn : IntrinsicFunction
{
    public readonly Value Value;

    private LLVMModuleRef _module;

    public ConstRetFn(string name, LLVMModuleRef module, Value value)
        : base(name,
            new FuncType(value.Type,
                Array.Empty<ClassType>(),
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
        LLVMValueRef func = _module.AddFunction(Name, ClassType.LLVMType);

        using LLVMBuilderRef builder = _module.Context.CreateBuilder();
        builder.PositionAtEnd(func.AppendBasicBlock(""));
        builder.BuildRet(Value.LLVMValue);

        return func;
    }
}

public sealed class Pow : IntrinsicFunction
{
    public Pow(string name, LLVMModuleRef module, ClassType retType, ClassType left, ClassType right)
        : base(name,
            new FuncType(retType,
                new ClassType[2] { left, right },
                LLVMTypeRef.CreateFunction(retType.LLVMType,
                    new LLVMTypeRef[2] { left.LLVMType, right.LLVMType })))
        => InternalLLVMValue = module.AddFunction(name, ClassType.LLVMType);

    public override Value Call(LLVMCompiler compiler, Value[] args)
        => new Value(ClassType.ReturnType, compiler.Builder.BuildCall2(ClassType.LLVMType, LLVMValue, args.AsLLVMValues(), "pow"));
}