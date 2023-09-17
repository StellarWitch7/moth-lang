using LLVMSharp.Interop;
using Moth.AST.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class Parameter
{
    public int ParamIndex { get; set; }
    public string Name { get; set; }
    public LLVMTypeRef LLVMType { get; set; }
    public DefinitionType Type { get; set; }
    public ClassRefNode TypeRef { get; set; }

    public Parameter(int paramIndex, string name, LLVMTypeRef lLVMType, DefinitionType type, ClassRefNode typeRef)
    {
        ParamIndex = paramIndex;
        Name = name;
        LLVMType = lLVMType;
        Type = type;
        TypeRef = typeRef;
    }
}
