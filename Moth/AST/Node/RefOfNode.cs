namespace Moth.AST.Node;

public class RefOfNode : SingleExprNode
{
    public RefOfNode(IExpressionNode value)
        : base(value) { }

    public override string GetSource()
    {
        return $"&{Expression.GetSource()}";
    }
}
