namespace Moth.LLVM.Data;

public sealed class ConstRetFn : IntrinsicFunction
{
    private Value _value { get; }
    private LLVMModuleRef _module { get; }
    
    public ConstRetFn(string name, Value value, LLVMModuleRef module)
        : base(new FuncType(name, value.Type, new Type[]{}, false, null))
    {
        if (!value.LLVMValue.IsConstant)
        {
            throw new ArgumentException("Value needs to be constant.");
        }

        _value = value;
        _module = module;
    }

    public override Value Call(LLVMCompiler compiler, Value[] args) => _value;

    protected override LLVMValueRef GenerateLLVMData()
    {
        LLVMValueRef func = _module.AddFunction(Type.Name, _value.Type.LLVMType);

        using LLVMBuilderRef builder = _module.Context.CreateBuilder();
        builder.PositionAtEnd(func.AppendBasicBlock(""));
        builder.BuildRet(_value.LLVMValue);

        return func;
    }
}

public sealed class Pow : IntrinsicFunction
{
    private LLVMModuleRef _module { get; }

    public Pow(string name, LLVMModuleRef module, Type retType, Type left, Type right)
        : base(new FuncType(name, retType, new Type[] { left, right }, false, null))
    {
        _module = module;
    }

    public override Value Call(LLVMCompiler compiler, Value[] args)
        => Type.Call(compiler, LLVMValue, args, "pow");
    
    protected override LLVMValueRef GenerateLLVMData()
    {
        try
        {
            return _module.GetNamedFunction(Type.Name);
        }
        catch
        {
            return _module.AddFunction(Type.Name, Type.LLVMType);
        }
    }
}