using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class ReferenceNode : ExpressionNode
{
    public ExpressionNode Value { get; set; }

    public ReferenceNode(ExpressionNode value)
    {
        Value = value;
    }
}
