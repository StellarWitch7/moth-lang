using Moth.AST.Node;

namespace Moth.LLVM.Data;

public sealed class Addition : IntrinsicOperator
{
    public Addition(
        LLVMCompiler compiler,
        PrimitiveStructDecl retStructDecl,
        PrimitiveStructDecl leftStructDecl,
        PrimitiveStructDecl rightStructDecl
    )
        : base(compiler, OperationType.Addition, retStructDecl, leftStructDecl, rightStructDecl) { }

    protected override LLVMValueRef OpInt(Value leftVal, Value rightVal)
    {
        return _compiler.Builder.BuildAdd(leftVal.LLVMValue, rightVal.LLVMValue);
    }

    protected override LLVMValueRef OpFloat(Value leftVal, Value rightVal)
    {
        return _compiler.Builder.BuildFAdd(leftVal.LLVMValue, rightVal.LLVMValue);
    }
}

public sealed class Subtraction : IntrinsicOperator
{
    public Subtraction(
        LLVMCompiler compiler,
        PrimitiveStructDecl retStructDecl,
        PrimitiveStructDecl leftStructDecl,
        PrimitiveStructDecl rightStructDecl
    )
        : base(compiler, OperationType.Subtraction, retStructDecl, leftStructDecl, rightStructDecl)
    { }

    protected override LLVMValueRef OpInt(Value leftVal, Value rightVal)
    {
        return _compiler.Builder.BuildSub(leftVal.LLVMValue, rightVal.LLVMValue);
    }

    protected override LLVMValueRef OpFloat(Value leftVal, Value rightVal)
    {
        return _compiler.Builder.BuildFSub(leftVal.LLVMValue, rightVal.LLVMValue);
    }
}

public sealed class Multiplication : IntrinsicOperator
{
    public Multiplication(
        LLVMCompiler compiler,
        PrimitiveStructDecl retStructDecl,
        PrimitiveStructDecl leftStructDecl,
        PrimitiveStructDecl rightStructDecl
    )
        : base(
            compiler,
            OperationType.Multiplication,
            retStructDecl,
            leftStructDecl,
            rightStructDecl
        ) { }

    protected override LLVMValueRef OpInt(Value leftVal, Value rightVal)
    {
        return _compiler.Builder.BuildMul(leftVal.LLVMValue, rightVal.LLVMValue);
    }

    protected override LLVMValueRef OpFloat(Value leftVal, Value rightVal)
    {
        return _compiler.Builder.BuildFMul(leftVal.LLVMValue, rightVal.LLVMValue);
    }
}

public sealed class Division : IntrinsicOperator
{
    public Division(
        LLVMCompiler compiler,
        PrimitiveStructDecl retStructDecl,
        PrimitiveStructDecl leftStructDecl,
        PrimitiveStructDecl rightStructDecl
    )
        : base(compiler, OperationType.Division, retStructDecl, leftStructDecl, rightStructDecl) { }

    protected override LLVMValueRef OpInt(Value leftVal, Value rightVal)
    {
        if (leftVal.Type is SignedInt)
        {
            return _compiler.Builder.BuildSDiv(leftVal.LLVMValue, rightVal.LLVMValue);
        }

        return _compiler.Builder.BuildUDiv(leftVal.LLVMValue, rightVal.LLVMValue);
    }

    protected override LLVMValueRef OpFloat(Value leftVal, Value rightVal)
    {
        return _compiler.Builder.BuildFDiv(leftVal.LLVMValue, rightVal.LLVMValue);
    }
}

//TODO
// public sealed class Exponential : IntrinsicOperator
// {
//
// }

public sealed class Modulus : IntrinsicOperator
{
    public Modulus(
        LLVMCompiler compiler,
        PrimitiveStructDecl retStructDecl,
        PrimitiveStructDecl leftStructDecl,
        PrimitiveStructDecl rightStructDecl
    )
        : base(compiler, OperationType.Modulus, retStructDecl, leftStructDecl, rightStructDecl) { }

    protected override LLVMValueRef OpInt(Value leftVal, Value rightVal)
    {
        if (leftVal.Type is SignedInt)
        {
            return _compiler.Builder.BuildSRem(leftVal.LLVMValue, rightVal.LLVMValue);
        }

        return _compiler.Builder.BuildURem(leftVal.LLVMValue, rightVal.LLVMValue);
    }

    protected override LLVMValueRef OpFloat(Value leftVal, Value rightVal)
    {
        return _compiler.Builder.BuildFRem(leftVal.LLVMValue, rightVal.LLVMValue);
    }
}

public sealed class LesserThan : IntrinsicOperator
{
    public LesserThan(
        LLVMCompiler compiler,
        PrimitiveStructDecl retStructDecl,
        PrimitiveStructDecl leftStructDecl,
        PrimitiveStructDecl rightStructDecl
    )
        : base(compiler, OperationType.LesserThan, retStructDecl, leftStructDecl, rightStructDecl)
    { }

    protected override LLVMValueRef OpInt(Value leftVal, Value rightVal)
    {
        if (leftVal.Type is SignedInt)
        {
            return _compiler.Builder.BuildICmp(
                LLVMIntPredicate.LLVMIntSLT,
                leftVal.LLVMValue,
                rightVal.LLVMValue
            );
        }

        return _compiler.Builder.BuildICmp(
            LLVMIntPredicate.LLVMIntULT,
            leftVal.LLVMValue,
            rightVal.LLVMValue
        );
    }

    protected override LLVMValueRef OpFloat(Value leftVal, Value rightVal)
    {
        return _compiler.Builder.BuildFCmp(
            LLVMRealPredicate.LLVMRealOLT,
            leftVal.LLVMValue,
            rightVal.LLVMValue
        );
    }
}

public sealed class LesserThanOrEqual : IntrinsicOperator
{
    public LesserThanOrEqual(
        LLVMCompiler compiler,
        PrimitiveStructDecl retStructDecl,
        PrimitiveStructDecl leftStructDecl,
        PrimitiveStructDecl rightStructDecl
    )
        : base(
            compiler,
            OperationType.LesserThanOrEqual,
            retStructDecl,
            leftStructDecl,
            rightStructDecl
        ) { }

    protected override LLVMValueRef OpInt(Value leftVal, Value rightVal)
    {
        if (leftVal.Type is SignedInt)
        {
            return _compiler.Builder.BuildICmp(
                LLVMIntPredicate.LLVMIntSLE,
                leftVal.LLVMValue,
                rightVal.LLVMValue
            );
        }

        return _compiler.Builder.BuildICmp(
            LLVMIntPredicate.LLVMIntULE,
            leftVal.LLVMValue,
            rightVal.LLVMValue
        );
    }

    protected override LLVMValueRef OpFloat(Value leftVal, Value rightVal)
    {
        return _compiler.Builder.BuildFCmp(
            LLVMRealPredicate.LLVMRealOLE,
            leftVal.LLVMValue,
            rightVal.LLVMValue
        );
    }
}

public sealed class GreaterThan : IntrinsicOperator
{
    public GreaterThan(
        LLVMCompiler compiler,
        PrimitiveStructDecl retStructDecl,
        PrimitiveStructDecl leftStructDecl,
        PrimitiveStructDecl rightStructDecl
    )
        : base(compiler, OperationType.GreaterThan, retStructDecl, leftStructDecl, rightStructDecl)
    { }

    protected override LLVMValueRef OpInt(Value leftVal, Value rightVal)
    {
        if (leftVal.Type is SignedInt)
        {
            return _compiler.Builder.BuildICmp(
                LLVMIntPredicate.LLVMIntSGT,
                leftVal.LLVMValue,
                rightVal.LLVMValue
            );
        }

        return _compiler.Builder.BuildICmp(
            LLVMIntPredicate.LLVMIntUGT,
            leftVal.LLVMValue,
            rightVal.LLVMValue
        );
    }

    protected override LLVMValueRef OpFloat(Value leftVal, Value rightVal)
    {
        return _compiler.Builder.BuildFCmp(
            LLVMRealPredicate.LLVMRealOGT,
            leftVal.LLVMValue,
            rightVal.LLVMValue
        );
    }
}

public sealed class GreaterThanOrEqual : IntrinsicOperator
{
    public GreaterThanOrEqual(
        LLVMCompiler compiler,
        PrimitiveStructDecl retStructDecl,
        PrimitiveStructDecl leftStructDecl,
        PrimitiveStructDecl rightStructDecl
    )
        : base(
            compiler,
            OperationType.GreaterThanOrEqual,
            retStructDecl,
            leftStructDecl,
            rightStructDecl
        ) { }

    protected override LLVMValueRef OpInt(Value leftVal, Value rightVal)
    {
        if (leftVal.Type is SignedInt)
        {
            return _compiler.Builder.BuildICmp(
                LLVMIntPredicate.LLVMIntSGE,
                leftVal.LLVMValue,
                rightVal.LLVMValue
            );
        }

        return _compiler.Builder.BuildICmp(
            LLVMIntPredicate.LLVMIntUGE,
            leftVal.LLVMValue,
            rightVal.LLVMValue
        );
    }

    protected override LLVMValueRef OpFloat(Value leftVal, Value rightVal)
    {
        return _compiler.Builder.BuildFCmp(
            LLVMRealPredicate.LLVMRealOGE,
            leftVal.LLVMValue,
            rightVal.LLVMValue
        );
    }
}

public sealed class Equal : IntrinsicOperator
{
    public Equal(
        LLVMCompiler compiler,
        PrimitiveStructDecl retStructDecl,
        PrimitiveStructDecl leftStructDecl,
        PrimitiveStructDecl rightStructDecl
    )
        : base(compiler, OperationType.Equal, retStructDecl, leftStructDecl, rightStructDecl) { }

    protected override LLVMValueRef OpInt(Value leftVal, Value rightVal)
    {
        if (rightVal.Type.Equals(_compiler.Null))
        {
            return _compiler.Builder.BuildIsNull(leftVal.LLVMValue);
        }

        return _compiler.Builder.BuildICmp(
            LLVMIntPredicate.LLVMIntEQ,
            leftVal.LLVMValue,
            rightVal.LLVMValue
        );
    }

    protected override LLVMValueRef OpFloat(Value leftVal, Value rightVal)
    {
        if (rightVal.Type.Equals(_compiler.Null))
        {
            return _compiler.Builder.BuildIsNull(leftVal.LLVMValue);
        }

        return _compiler.Builder.BuildFCmp(
            LLVMRealPredicate.LLVMRealOEQ,
            leftVal.LLVMValue,
            rightVal.LLVMValue
        );
    }
}

// public sealed class Range : IntrinsicOperator
// {
//     public Range(PrimitiveType retType, PrimitiveType leftType, PrimitiveType rightType)
//         : base(compiler, OperationType.Range, retType, leftType, rightType)
//     {
//
//     }
// }
