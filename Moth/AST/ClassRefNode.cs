using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST;

public class ClassRefNode : RefNode
{
    public bool IsCurrentClass { get; }

    public ClassRefNode(bool isCurrentClass, string name) : base(name)
    {
        IsCurrentClass = isCurrentClass;
    }
}
