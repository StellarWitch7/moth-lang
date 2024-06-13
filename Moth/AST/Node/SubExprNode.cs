namespace Moth.AST.Node;

public class SubExprNode : SingleExprNode
{
    public SubExprNode(IExpressionNode expression)
        : base(expression) { }

    public override string GetSource()
    {
        return $"({Expression.GetSource()})";
    }
}
