using LLVMSharp.Interop;
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

public sealed class LlvmFunction : Function
{
    public override LLVMValueRef LLVMFunc { get; }
    public override LLVMTypeRef LLVMFuncType { get; }
    public readonly PrivacyType Privacy;
    public readonly Class? OwnerClass;
    public Scope? OpeningScope { get; set; }
    public readonly IReadOnlyList<Parameter> Params;
    public readonly bool IsVariadic;

    public LlvmFunction(string name, LLVMValueRef llvmFunc, LLVMTypeRef lLvmFuncType, Type returnType, PrivacyType privacy, Class? ownerClass, IReadOnlyList<Parameter> @params, bool isVariadic) : base(name, returnType)
    {
        LLVMFunc = llvmFunc;
        LLVMFuncType = lLvmFuncType;
        Privacy = privacy;
        OwnerClass = ownerClass;
        Params = @params;
        IsVariadic = isVariadic;
    }
    
    public override LLVMValueRef Call(LLVMBuilderRef builder, ReadOnlySpan<LLVMValueRef> parameters)
    {
        return builder.BuildCall2(LLVMFuncType, LLVMFunc, parameters, "");
    }
}

public abstract class IntrinsicFunction : Function
{
    protected LLVMValueRef InternalLlvmFunc;
    protected LLVMTypeRef InternalLlvmFuncType;
    
    protected IntrinsicFunction(string name, Type returnType) : base(name, returnType) {}
    
    public override LLVMValueRef LLVMFunc
    {
        get
        {
            if (InternalLlvmFunc == default)
                (InternalLlvmFunc, InternalLlvmFuncType) = GenerateLlvmData();

            return InternalLlvmFunc;
        }
    }
    public override LLVMTypeRef LLVMFuncType{
        get
        {
            if (InternalLlvmFuncType == default)
                (InternalLlvmFunc, InternalLlvmFuncType) = GenerateLlvmData();

            return InternalLlvmFuncType;
        }
    }

    protected virtual (LLVMValueRef, LLVMTypeRef) GenerateLlvmData()
    {
        throw new NotImplementedException("This function does not support LLVM data generation.");
    }
}