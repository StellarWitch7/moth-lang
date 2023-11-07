namespace Moth.AST.Node;

public class WhileNode : StatementNode
{
    public ExpressionNode Condition { get; set; }
    public ScopeNode Then { get; set; }

    public WhileNode(ExpressionNode condition, ScopeNode then)
    {
        Condition = condition;
        Then = then;
    }
}
