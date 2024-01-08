namespace Moth.LLVM.Data;

public class Array : Value
{
    public override ArrType Type { get; }
    public override LLVMValueRef LLVMValue { get; }

    private LLVMCompiler _compiler;

    public Array(LLVMCompiler compiler, Type elementType, Value[] elements)
        : base(null, null)
    {
        _compiler = compiler;
        Type = new ArrType(compiler, elementType);
        LLVMValue = compiler.Builder.BuildAlloca(Type.BaseType.LLVMType);
        
        var arr = compiler.Builder.BuildStructGEP2(Type.BaseType.LLVMType, LLVMValue, 0);
        var length = compiler.Builder.BuildStructGEP2(Type.BaseType.LLVMType, LLVMValue, 1);
        compiler.Builder.BuildStore(LLVMValueRef.CreateConstArray(elementType.LLVMType, elements.AsLLVMValues()), arr);
        compiler.Builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)elements.Length), length);
    }
}
