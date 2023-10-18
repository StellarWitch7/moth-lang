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
    public Type Type { get; set; }
    public PrivacyType Privacy { get; set; }
    public Class ClassOfType { get; set; }

    public Field(string name, uint index, Type type, Class classOfType, PrivacyType privacy)
    {
        Name = name;
        FieldIndex = index;
        Type = type;
        Privacy = privacy;
        ClassOfType = classOfType;
    }
}
