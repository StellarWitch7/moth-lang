using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM.Data;

public class Constant
{
    public LLVMTypeRef LLVMType { get; set; }
    public LLVMValueRef LLVMValue { get; set; }
    public Class ClassOfType { get; set; }

    public Constant(LLVMTypeRef lLVMType, LLVMValueRef value, Class classOfType)
    {
        LLVMType = lLVMType;
        LLVMValue = value;
        ClassOfType = classOfType;
    }
}
