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
    public string Name { get; set; }
    public LLVMValueRef LLVMVariable { get; set; }
    public Type Type { get; set; }
    public Class ClassOfType { get; set; }

    public Variable(string name, LLVMValueRef lLVMVariable, Type type, Class classOfType)
    {
        Name = name;
        LLVMVariable = lLVMVariable;
        Type = type;
        ClassOfType = classOfType;
    }
}
