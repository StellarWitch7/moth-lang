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
    public Type Type { get; set; }
    public LLVMValueRef LLVMValue { get; set; }

    public ValueContext(Type type, LLVMValueRef value)
    {
        Type = type;
        LLVMValue = value;
    }
}
