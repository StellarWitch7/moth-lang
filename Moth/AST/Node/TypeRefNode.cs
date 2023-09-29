using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class TypeRefNode
{
    string Type { get; }
    bool IsPointer { get; }

    public TypeRefNode(string type, bool isPointer)
    {
        Type = type;
        IsPointer = isPointer;
    }
}
