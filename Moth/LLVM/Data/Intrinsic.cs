using LLVMSharp.Interop;

namespace Moth.LLVM.Data;

public class Intrinsic : CompilerData
{
    public string Name { get; set; }
    public LLVMValueRef LLVMFunc { get; set; }
    public LLVMTypeRef LLVMFuncType { get; set; }

    public Intrinsic(string name, LLVMValueRef llvmFunc, LLVMTypeRef llvmFuncType)
    {
        Name = name;
        LLVMFunc = llvmFunc;
        LLVMFuncType = llvmFuncType;
    }
}
