using LLVMSharp.Interop;
using Moth.LLVM.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class ValueContext
{
    public LLVMTypeRef LLVMType { get; set; }
    public LLVMValueRef LLVMValue { get; set; }
    public Class ClassOfType { get; set; }
    public bool IsVariable { get; set; }

    public ValueContext(LLVMTypeRef type, LLVMValueRef value, Class classOfType, bool isVariable)
    {
        LLVMType = type;
        LLVMValue = value;
        ClassOfType = classOfType;
        IsVariable = isVariable;
    }
}
