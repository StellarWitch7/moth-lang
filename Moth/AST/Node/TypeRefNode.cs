using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class TypeRefNode : RefNode
{
    public int PointerDepth { get; set; }

    public TypeRefNode(string name, int pointerDepth = 0) : base(name)
    {
        PointerDepth = pointerDepth;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(Name);

        for (int i = 0; i < PointerDepth; i++)
        {
            builder.Append('*');
        }

        return builder.ToString();
    }
}
