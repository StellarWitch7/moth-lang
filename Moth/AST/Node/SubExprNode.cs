using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class SubExprNode : ExpressionNode
{
    public ExpressionNode Expression { get; set; }

    public SubExprNode(ExpressionNode expression)
    {
        Expression = expression;
    }
}
