﻿using Moth.AST.Node;

namespace Moth.LLVM.Data;

public abstract class IntrinsicOperator : IntrinsicFunction
{
    protected PrimitiveStructDecl RetStructDecl { get; }
    protected PrimitiveStructDecl LeftStructDecl { get; }
    protected PrimitiveStructDecl RightStructDecl { get; }

    public IntrinsicOperator(
        LLVMCompiler compiler,
        OperationType opType,
        PrimitiveStructDecl retStructDecl,
        PrimitiveStructDecl leftStructDecl,
        PrimitiveStructDecl rightStructDecl
    )
        : base(
            compiler,
            Utils.ExpandOpName(Utils.OpTypeToString(opType)),
            new FuncType(
                compiler,
                retStructDecl,
                new Type[] { new PtrType(compiler, leftStructDecl), rightStructDecl },
                false
            )
        )
    {
        RetStructDecl = retStructDecl;
        LeftStructDecl = leftStructDecl;
        RightStructDecl = rightStructDecl;
    }

    protected override LLVMValueRef GenerateLLVMData() =>
        throw new NotImplementedException("Intrinsic operator does not return function.");

    public override Value Call(Value[] args)
    {
        if (args.Length != 2)
        {
            throw new Exception("Intrinsic operator must be called with exactly two arguments.");
        }

        var leftVal = args[0].DeRef();
        var rightVal = args[1];

        if (!leftVal.Type.Equals(LeftStructDecl))
        {
            throw new Exception("Left operand of intrinsic operator is not of the expected type.");
        }

        if (!rightVal.Type.Equals(RightStructDecl))
        {
            throw new Exception("Right operand of intrinsic operator is not of the expected type.");
        }

        if (rightVal.Type.CanConvertTo(leftVal.Type))
        {
            rightVal = rightVal.ImplicitConvertTo(leftVal.Type);
        }
        else if (leftVal.Type.CanConvertTo(rightVal.Type))
        {
            leftVal = leftVal.ImplicitConvertTo(rightVal.Type);
        }
        else
        {
            throw new Exception(
                "Intrinsic operator's operand type cannot be made to match using implicit conversions."
            );
        }

        var leftType = leftVal.Type;
        var rightType = rightVal.Type;
        LLVMValueRef value;

        if (leftType is Float)
        {
            value = OpFloat(leftVal, rightVal);
        }
        else if (leftType is Int)
        {
            value = OpInt(leftVal, rightVal);
        }
        else
        {
            throw new NotImplementedException("Unsupported primitive type for intrinsic operator.");
        }

        return Value.Create(_compiler, RetStructDecl, value);
    }

    protected abstract LLVMValueRef OpFloat(Value leftVal, Value rightVal);
    protected abstract LLVMValueRef OpInt(Value leftVal, Value rightVal);
}

public sealed class ConstRetFn : IntrinsicFunction
{
    private Value _value { get; }
    private LLVMModuleRef _module { get; }

    public ConstRetFn(LLVMCompiler compiler, string name, Value value, LLVMModuleRef module)
        : base(compiler, name, new FuncType(compiler, value.Type, new Type[] { }, false))
    {
        if (!value.LLVMValue.IsConstant)
        {
            throw new ArgumentException("Value needs to be constant.");
        }

        _value = value;
        _module = module;
    }

    public override Value Call(Value[] args) => _value;

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

    public Pow(
        LLVMCompiler compiler,
        string name,
        LLVMModuleRef module,
        Type retType,
        Type left,
        Type right
    )
        : base(compiler, name, new FuncType(compiler, retType, new Type[] { left, right }, false))
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
