using LLVMSharp.Interop;
using Moth.AST.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class Parameter : CompilerData
{
    public int ParamIndex { get; set; }
    public string Name { get; set; }
    public LLVMTypeRef LLVMType { get; set; }
    public TypeRefNode TypeRef { get; set; }

    public Parameter(int paramIndex, string name, LLVMTypeRef lLVMType, TypeRefNode typeRef)
    {
        ParamIndex = paramIndex;
        Name = name;
        LLVMType = lLVMType;
        TypeRef = typeRef;
    }
}
