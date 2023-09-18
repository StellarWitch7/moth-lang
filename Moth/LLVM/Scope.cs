using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class Scope : CompilerData
{
    public LLVMBasicBlockRef LLVMBlock { get; set; }
    public Dictionary<string, Variable> Variables { get; set; } = new Dictionary<string, Variable>();

    public Scope(LLVMBasicBlockRef lLVMBlock)
    {
        LLVMBlock = lLVMBlock;
    }
}
