using LLVMSharp.Interop;
using Moth.AST.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class Field : CompilerData
{
    public uint FieldIndex { get; set; }
    public LLVMTypeRef LLVMType { get; set; }
    public PrivacyType Privacy { get; set; }
    public TypeRefNode TypeRef { get; set; }
    public bool IsConstant { get; set; }

    public Field(uint index, LLVMTypeRef lLVMType, PrivacyType privacy,
        TypeRefNode typeRef, bool isConstant)
    {
        FieldIndex = index;
        LLVMType = lLVMType;
        Privacy = privacy;
        TypeRef = typeRef;
        IsConstant = isConstant;
    }
}
