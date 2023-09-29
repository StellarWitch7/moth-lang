using LLVMSharp.Interop;
using Moth.AST.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM.Data;

public class Variable : CompilerData
{
    public LLVMValueRef LLVMVariable { get; set; }
    public LLVMTypeRef LLVMType { get; set; }
    public PrivacyType Privacy { get; set; }
    public Class ClassOfType { get; set; }
    public bool IsConstant { get; set; }

    public Variable(LLVMValueRef lLVMVariable, LLVMTypeRef lLVMType, PrivacyType privacy,
        Class classOfType, bool isConstant)
    {
        LLVMVariable = lLVMVariable;
        LLVMType = lLVMType;
        Privacy = privacy;
        ClassOfType = classOfType;
        IsConstant = isConstant;
    }
}
