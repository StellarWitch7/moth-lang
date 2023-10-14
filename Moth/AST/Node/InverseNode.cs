using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class InverseNode : ExpressionNode
{
    public RefNode Value { get; set; }

    public InverseNode(RefNode value)
    {
        Value = value;
    }
}
