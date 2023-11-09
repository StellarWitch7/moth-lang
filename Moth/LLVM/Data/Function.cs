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

public sealed class LLVMFunction : Function
{
    public override LLVMValueRef LLVMFunc { get; }
    public override LLVMTypeRef LLVMFuncType { get; }
    public readonly PrivacyType Privacy;
    public readonly Class? OwnerClass;
    public Scope? OpeningScope { get; set; }
    public readonly IReadOnlyList<Parameter> Params;
    public readonly bool IsVariadic;

    public LLVMFunction(string name, LLVMValueRef llvmFunc, LLVMTypeRef llvmFuncType, Type returnType, PrivacyType privacy, Class? ownerClass, IReadOnlyList<Parameter> @params, bool isVariadic) : base(name, returnType)
    {
        LLVMFunc = llvmFunc;
        LLVMFuncType = llvmFuncType;
        Privacy = privacy;
        OwnerClass = ownerClass;
        Params = @params;
        IsVariadic = isVariadic;
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

    protected virtual (LLVMValueRef, LLVMTypeRef) GenerateLLVMData() => throw new NotImplementedException("This function does not support LLVM data generation.");
}