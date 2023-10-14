using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class TypeRefNode : RefNode
{
    public bool IsPointer { get; set; }

    public TypeRefNode(string name, bool isPointer) : base(name)
    {
        IsPointer = isPointer;
    }
}
