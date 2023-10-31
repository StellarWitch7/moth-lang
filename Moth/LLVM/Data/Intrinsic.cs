using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM.Data;

public class Intrinsic : CompilerData
{
    public string Name { get; set; }
    public LLVMValueRef LLVMFunc { get; set; }
    public LLVMTypeRef LLVMFuncType { get; set; }

    public Intrinsic(string name, LLVMValueRef lLVMFunc, LLVMTypeRef lLVMFuncType)
    {
        Name = name;
        LLVMFunc = lLVMFunc;
        LLVMFuncType = lLVMFuncType;
    }
}
