using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class ValueContext
{
    public LLVMTypeRef LLVMType { get; set; }
    public LLVMValueRef Value { get; set; }

    public ValueContext(LLVMTypeRef type, LLVMValueRef value)
    {
        LLVMType = type;
        Value = value;
    }
}
