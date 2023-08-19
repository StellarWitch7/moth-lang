using LLVMSharp.Interop;
using Moth.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class Variable
{
    public LLVMValueRef LLVMVariable { get; set; }
    public PrivacyType Privacy { get; set; }
    public bool IsConstant { get; set; }

    public Variable(LLVMValueRef lLVMVariable, PrivacyType privacy, bool isConstant)
    {
        LLVMVariable = lLVMVariable;
        Privacy = privacy;
        IsConstant = isConstant;
    }
}
