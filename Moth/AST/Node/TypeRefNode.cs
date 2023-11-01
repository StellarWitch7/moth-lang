using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class TypeRefNode : RefNode
{
    public uint PointerDepth { get; set; }

    public TypeRefNode(string name, uint pointerDepth = 0) : base(name)
    {
        PointerDepth = pointerDepth;
    }
}
