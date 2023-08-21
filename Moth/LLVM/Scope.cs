using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class Scope
{
    public LLVMBasicBlockRef LLVMBlock { get; set; }
    public Dictionary<string, Variable> Variables { get; set; }

    public Scope(LLVMBasicBlockRef lLVMBlock)
    {
        LLVMBlock = lLVMBlock;
    }
}
