using Moth.LLVM.Data;

namespace Moth.LLVM;

public sealed class ConstRetFn : Intrinsic
{
    private Value _value { get; }
    
    public ConstRetFn(string name, Value value)
        : base(new LocalFunction(value.Type, new Type[]{}, new Parameter[]{}))
    {
        if (!value.LLVMValue.IsConstant)
        {
            throw new ArgumentException("Value needs to be constant.");
        }

        _value = value;
    }

    public override Value Call(LLVMCompiler compiler, Value[] args) => _value;

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