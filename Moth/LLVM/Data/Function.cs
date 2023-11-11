using Moth.AST.Node;

namespace Moth.LLVM.Data;

public abstract class Function : CompilerData
{
    public readonly string Name;
    public readonly Type ReturnType;

    public abstract LLVMValueRef LLVMFunc { get; }
    public abstract LLVMTypeRef LLVMFuncType { get; }

    protected Function(string name, Type returnType)
    {
        Name = name;
        ReturnType = returnType;
    }

    public abstract LLVMValueRef Call(LLVMBuilderRef builder, ReadOnlySpan<LLVMValueRef> parameters);
}

public class LLVMFunction : Function
{
    public override LLVMValueRef LLVMFunc { get; }
    public override LLVMTypeRef LLVMFuncType { get; }
    public Scope? OpeningScope { get; set; }

    public readonly IReadOnlyList<Parameter> Params;
    public readonly bool IsVariadic;

    public LLVMFunction(string name, LLVMValueRef llvmFunc,
        LLVMTypeRef llvmFuncType, Type returnType,
        IReadOnlyList<Parameter> @params, bool isVariadic)
        : base(name, returnType)
    {
        LLVMFunc = llvmFunc;
        LLVMFuncType = llvmFuncType;
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

    public override LLVMValueRef Call(LLVMBuilderRef builder, ReadOnlySpan<LLVMValueRef> parameters)
    {
        return builder.BuildCall2(LLVMFuncType,
            LLVMFunc,
            parameters,
            ReturnType.LLVMType.Kind != LLVMTypeKind.LLVMVoidTypeKind
                ? Name
                : "");
    }
}

public sealed class DefinedFunction : LLVMFunction
{
    public new Class? OwnerClass { get; set; }

    public readonly PrivacyType Privacy;

    public DefinedFunction(string name, LLVMValueRef llvmFunc,
        LLVMTypeRef llvmFuncType, Type returnType,
        PrivacyType privacy, Class? ownerClass,
        IReadOnlyList<Parameter> @params, bool isVariadic)
        : base(name, llvmFunc, llvmFuncType, returnType, @params, isVariadic)
    {
        Privacy = privacy;
        OwnerClass = ownerClass;
    }
}

public sealed class LocalFunction : LLVMFunction
{
    public LocalFunction(LLVMValueRef llvmFunc, LLVMTypeRef llvmFuncType, Type returnType, IReadOnlyList<Parameter> @params)
        : base("localfunc", llvmFunc, llvmFuncType, returnType, @params, false) { }
}

public abstract class IntrinsicFunction : Function
{
    protected LLVMValueRef InternalLLVMFunc;
    protected LLVMTypeRef InternalLLVMFuncType;

    protected IntrinsicFunction(string name, Type returnType) : base(name, returnType) { }

    public override LLVMValueRef LLVMFunc
    {
        get
        {
            if (InternalLLVMFunc == default)
            {
                (InternalLLVMFunc, InternalLLVMFuncType) = GenerateLLVMData();
            }

            return InternalLLVMFunc;
        }
    }
    public override LLVMTypeRef LLVMFuncType
    {
        get
        {
            if (InternalLLVMFuncType == default)
            {
                (InternalLLVMFunc, InternalLLVMFuncType) = GenerateLLVMData();
            }

            return InternalLLVMFuncType;
        }
    }

    protected virtual (LLVMValueRef, LLVMTypeRef) GenerateLLVMData()
        => throw new NotImplementedException("This function does not support LLVM data generation.");
}