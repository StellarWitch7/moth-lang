using Moth.AST.Node;

namespace Moth.LLVM.Data;

public sealed class Addition : IntrinsicOperator
{
    public Addition(PrimitiveType retType, PrimitiveType leftType, PrimitiveType rightType)
        : base(OperationType.Addition, retType, leftType, rightType) { }

    protected override LLVMValueRef OpInt(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        return compiler.Builder.BuildAdd(leftVal.LLVMValue, rightVal.LLVMValue);
    }

    protected override LLVMValueRef OpFloat(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        return compiler.Builder.BuildFAdd(leftVal.LLVMValue, rightVal.LLVMValue);
    }
}

public sealed class Subtraction : IntrinsicOperator
{
    public Subtraction(PrimitiveType retType, PrimitiveType leftType, PrimitiveType rightType)
        : base(OperationType.Subtraction, retType, leftType, rightType) { }
    
    protected override LLVMValueRef OpInt(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        return compiler.Builder.BuildSub(leftVal.LLVMValue, rightVal.LLVMValue);
    }

    protected override LLVMValueRef OpFloat(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        return compiler.Builder.BuildFSub(leftVal.LLVMValue, rightVal.LLVMValue);
    }
}

public sealed class Multiplication : IntrinsicOperator
{
    public Multiplication(PrimitiveType retType, PrimitiveType leftType, PrimitiveType rightType)
        : base(OperationType.Multiplication, retType, leftType, rightType) { }
    
    protected override LLVMValueRef OpInt(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        return compiler.Builder.BuildMul(leftVal.LLVMValue, rightVal.LLVMValue);
    }

    protected override LLVMValueRef OpFloat(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        return compiler.Builder.BuildFMul(leftVal.LLVMValue, rightVal.LLVMValue);
    }
}

public sealed class Division : IntrinsicOperator
{
    public Division(PrimitiveType retType, PrimitiveType leftType, PrimitiveType rightType)
        : base(OperationType.Division, retType, leftType, rightType) { }
        
    protected override LLVMValueRef OpInt(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        if (leftVal.Type is SignedInt)
        {
            return compiler.Builder.BuildSDiv(leftVal.LLVMValue, rightVal.LLVMValue);
        }
        
        return compiler.Builder.BuildUDiv(leftVal.LLVMValue, rightVal.LLVMValue);
    }

    protected override LLVMValueRef OpFloat(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        return compiler.Builder.BuildFDiv(leftVal.LLVMValue, rightVal.LLVMValue);
    }
}

//TODO
// public sealed class Exponential : IntrinsicOperator
// {
//     
// }

public sealed class Modulus : IntrinsicOperator
{
    public Modulus(PrimitiveType retType, PrimitiveType leftType, PrimitiveType rightType)
        : base(OperationType.Modulus, retType, leftType, rightType) { }
    
    protected override LLVMValueRef OpInt(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        if (leftVal.Type is SignedInt)
        {
            return compiler.Builder.BuildSRem(leftVal.LLVMValue, rightVal.LLVMValue);
        }
        
        return compiler.Builder.BuildURem(leftVal.LLVMValue, rightVal.LLVMValue);
    }

    protected override LLVMValueRef OpFloat(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        return compiler.Builder.BuildFRem(leftVal.LLVMValue, rightVal.LLVMValue);
    }
}

public sealed class LesserThan : IntrinsicOperator
{
    public LesserThan(PrimitiveType retType, PrimitiveType leftType, PrimitiveType rightType)
        : base(OperationType.LesserThan, retType, leftType, rightType) { }
    
    protected override LLVMValueRef OpInt(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        if (leftVal.Type is SignedInt)
        {
            return compiler.Builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, leftVal.LLVMValue, rightVal.LLVMValue);
        }
        
        return compiler.Builder.BuildICmp(LLVMIntPredicate.LLVMIntULT, leftVal.LLVMValue, rightVal.LLVMValue);
    }

    protected override LLVMValueRef OpFloat(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        return compiler.Builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLT, leftVal.LLVMValue, rightVal.LLVMValue);
    }
}

public sealed class LesserThanOrEqual : IntrinsicOperator
{
    public LesserThanOrEqual(PrimitiveType retType, PrimitiveType leftType, PrimitiveType rightType)
        : base(OperationType.LesserThanOrEqual, retType, leftType, rightType) { }
    
    protected override LLVMValueRef OpInt(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        if (leftVal.Type is SignedInt)
        {
            return compiler.Builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, leftVal.LLVMValue, rightVal.LLVMValue);
        }
        
        return compiler.Builder.BuildICmp(LLVMIntPredicate.LLVMIntULE, leftVal.LLVMValue, rightVal.LLVMValue);
    }

    protected override LLVMValueRef OpFloat(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        return compiler.Builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLE, leftVal.LLVMValue, rightVal.LLVMValue);
    }
}

public sealed class GreaterThan : IntrinsicOperator
{
    public GreaterThan(PrimitiveType retType, PrimitiveType leftType, PrimitiveType rightType)
        : base(OperationType.GreaterThan, retType, leftType, rightType) { }
    
    protected override LLVMValueRef OpInt(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        if (leftVal.Type is SignedInt)
        {
            return compiler.Builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, leftVal.LLVMValue, rightVal.LLVMValue);
        }
        
        return compiler.Builder.BuildICmp(LLVMIntPredicate.LLVMIntUGT, leftVal.LLVMValue, rightVal.LLVMValue);
    }

    protected override LLVMValueRef OpFloat(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        return compiler.Builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGT, leftVal.LLVMValue, rightVal.LLVMValue);
    }
}

public sealed class GreaterThanOrEqual : IntrinsicOperator
{
    public GreaterThanOrEqual(PrimitiveType retType, PrimitiveType leftType, PrimitiveType rightType)
        : base(OperationType.GreaterThanOrEqual, retType, leftType, rightType) { }
    
    protected override LLVMValueRef OpInt(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        if (leftVal.Type is SignedInt)
        {
            return compiler.Builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, leftVal.LLVMValue, rightVal.LLVMValue);
        }
        
        return compiler.Builder.BuildICmp(LLVMIntPredicate.LLVMIntUGE, leftVal.LLVMValue, rightVal.LLVMValue);
    }

    protected override LLVMValueRef OpFloat(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        return compiler.Builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGE, leftVal.LLVMValue, rightVal.LLVMValue);
    }
}

public sealed class Equal : IntrinsicOperator
{
    public Equal(PrimitiveType retType, PrimitiveType leftType, PrimitiveType rightType)
        : base(OperationType.Equal, retType, leftType, rightType) { }
    
    protected override LLVMValueRef OpInt(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        return compiler.Builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, leftVal.LLVMValue, rightVal.LLVMValue);
    }

    protected override LLVMValueRef OpFloat(LLVMCompiler compiler, Value leftVal, Value rightVal)
    {
        return compiler.Builder.BuildFCmp(LLVMRealPredicate.LLVMRealOEQ, leftVal.LLVMValue, rightVal.LLVMValue);
    }
}

// public sealed class Range : IntrinsicOperator
// {
//     public Range(PrimitiveType retType, PrimitiveType leftType, PrimitiveType rightType)
//         : base(OperationType.Range, retType, leftType, rightType)
//     {
//         
//     }
// }