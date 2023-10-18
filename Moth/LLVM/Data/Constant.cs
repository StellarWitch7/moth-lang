using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM.Data;

public class Constant
{
    public Type Type { get; set; }
    public LLVMValueRef LLVMValue { get; set; }
    public Class ClassOfType { get; set; }

    public Constant(Type type, LLVMValueRef value, Class classOfType)
    {
        Type = type;
        LLVMValue = value;
        ClassOfType = classOfType;
    }
}
