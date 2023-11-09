namespace Moth.AST.Node;

public class SubExprNode : ExpressionNode
{
    public ExpressionNode Expression { get; set; }

    public SubExprNode(ExpressionNode expression) => Expression = expression;
}
