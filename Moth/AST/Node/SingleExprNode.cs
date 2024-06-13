namespace Moth.AST.Node;

public abstract class SingleExprNode : IExpressionNode
{
    public IExpressionNode Expression { get; set; }

    protected SingleExprNode(IExpressionNode expression) => Expression = expression;

    public abstract string GetSource();
}
