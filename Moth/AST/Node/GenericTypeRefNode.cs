using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class GenericTypeRefNode : TypeRefNode
{
    List<ExpressionNode> Arguments { get; set; }

    public GenericTypeRefNode(string name, List<ExpressionNode> args, uint pointerDepth = 0) : base(name, pointerDepth)
    {
        Arguments = args;
    }
}
