using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM.Data;

public class Scope : CompilerData
{
    public LLVMBasicBlockRef LLVMBlock { get; set; }
    public Dictionary<string, Variable> Variables { get; set; } = new Dictionary<string, Variable>();

    public Scope(LLVMBasicBlockRef lLVMBlock)
    {
        LLVMBlock = lLVMBlock;
    }

    public Variable GetVariable(string name)
    {
        if (Variables.TryGetValue(name, out Variable @var))
        {
            return @var;
        }
        else
        {
            throw new Exception($"Variable \"{name}\" does not exist in the current scope.");
        }
    }
}
