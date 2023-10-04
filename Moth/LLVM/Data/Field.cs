using LLVMSharp.Interop;
using Moth.AST.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM.Data;

public class Field : CompilerData
{
    public string Name { get; set; }
    public uint FieldIndex { get; set; }
    public LLVMTypeRef LLVMType { get; set; }
    public PrivacyType Privacy { get; set; }
    public Class ClassOfType { get; set; }

    public Field(string name, uint index, LLVMTypeRef lLVMType, Class classOfType, PrivacyType privacy)
    {
        Name = name;
        FieldIndex = index;
        LLVMType = lLVMType;
        Privacy = privacy;
        ClassOfType = classOfType;
    }
}
