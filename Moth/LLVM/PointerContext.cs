using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class PointerContext
{
    public LLVMTypeRef Type { get; set; }
    public LLVMValueRef Pointer { get; set; }

    public PointerContext(LLVMTypeRef type, LLVMValueRef pointer)
    {
        this.Type = type;
        this.Pointer = pointer;
    }
}