using LLVMSharp.Interop;
using Moth.Compiler.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class Function
{
    public LLVMValueRef LLVMFunc { get; set; }
    public PrivacyType Privacy { get; set; }

    public Function(LLVMValueRef lLVMFunc, PrivacyType privacy)
    {
        LLVMFunc = lLVMFunc;
        Privacy = privacy;
    }
}
