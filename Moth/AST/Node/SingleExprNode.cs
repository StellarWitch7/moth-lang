namespace Moth.AST.Node;

public abstract class SingleExprNode : IExpressionNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
    public IExpressionNode Expression { get; set; }

    protected SingleExprNode(IExpressionNode expression) => Expression = expression;

    public abstract string GetSource();
}
