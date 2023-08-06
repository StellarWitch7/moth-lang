using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST;

public class IfNode : StatementNode
{
    public ExpressionNode Condition { get; }
    public ScopeNode Then { get; }
    public ScopeNode? Else { get; }

    public IfNode(ExpressionNode condition, ScopeNode then, ScopeNode? @else)
    {
        Condition = condition;
        Then = then;
        Else = @else;
    }
}
