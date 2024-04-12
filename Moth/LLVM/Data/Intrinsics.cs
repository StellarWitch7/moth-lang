using Moth.AST.Node;

namespace Moth.LLVM.Data;

public abstract class IntrinsicOperator : IntrinsicFunction
{
    protected PrimitiveType RetType { get; }
    protected PrimitiveType LeftType { get; }
    protected PrimitiveType RightType { get; }

    public IntrinsicOperator(OperationType opType, PrimitiveType retType, PrimitiveType leftType, PrimitiveType rightType)
        : base(Utils.ExpandOpName(Utils.OpTypeToString(opType)),
            new FuncType(retType,
                new Type[]
                {
                    new PtrType(leftType),
                    rightType
                },
                false))
    {
        RetType = retType;
        LeftType = leftType;
        RightType = rightType;
    }
    
    protected override LLVMValueRef GenerateLLVMData() => throw new NotImplementedException("Intrinsic operator does not return function.");

    public override Value Call(LLVMCompiler compiler, Value[] args)
    {
        if (args.Length != 2)
        {
            throw new Exception("Intrinsic operator must be called with exactly two arguments.");
        }

        var leftVal = args[0].DeRef(compiler);
        var rightVal = args[1];

        if (!leftVal.Type.Equals(LeftType))
        {
            throw new Exception("Left operand of intrinsic operator is not of the expected type.");
        }

        if (!rightVal.Type.Equals(RightType))
        {
            throw new Exception("Right operand of intrinsic operator is not of the expected type.");
        }

        if (rightVal.Type.CanConvertTo(leftVal.Type))
        {
            rightVal = rightVal.ImplicitConvertTo(compiler, leftVal.Type);
        }
        else if (leftVal.Type.CanConvertTo(rightVal.Type))
        {
            leftVal = leftVal.ImplicitConvertTo(compiler, rightVal.Type);
        }
        else
        {
            throw new Exception("Intrinsic operator's operand type cannot be made to match using implicit conversions.");
        }
        
        var leftType = leftVal.Type;
        var rightType = rightVal.Type;
        LLVMValueRef value;

        if (leftType is Float)
        {
            value = OpFloat(compiler, leftVal, rightVal);
        }
        else if (leftType is Int)
        {
            value = OpInt(compiler, leftVal, rightVal);
        }
        else
        {
            throw new NotImplementedException("Unsupported primitive type for intrinsic operator.");
        }

        return Value.Create(RetType, value);
    }

    protected abstract LLVMValueRef OpFloat(LLVMCompiler compiler, Value leftVal, Value rightVal);
    protected abstract LLVMValueRef OpInt(LLVMCompiler compiler, Value leftVal, Value rightVal);
}

public sealed class ConstRetFn : IntrinsicFunction
{
    private Value _value { get; }
    private LLVMModuleRef _module { get; }
    
    public ConstRetFn(string name, Value value, LLVMModuleRef module)
        : base(name, new FuncType(value.Type, new Type[]{}, false))
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
        LLVMValueRef func = _module.AddFunction(Name, _value.Type.LLVMType);

        using LLVMBuilderRef builder = _module.Context.CreateBuilder();
        builder.PositionAtEnd(func.AppendBasicBlock("entry"));
        builder.BuildRet(_value.LLVMValue);

        return func;
    }
}

public sealed class Pow : IntrinsicFunction
{
    private LLVMModuleRef _module { get; }

    public Pow(string name, LLVMModuleRef module, Type retType, Type left, Type right)
        : base(name, new FuncType(retType, new Type[] { left, right }, false))
    {
        _module = module;
    }
    
    protected override LLVMValueRef GenerateLLVMData()
    {
        try
        {
            return _module.GetNamedFunction(Name);
        }
        catch
        {
            return _module.AddFunction(Name, Type.LLVMType);
        }
    }
}