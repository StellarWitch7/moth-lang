namespace Moth.AST.Node;

public class DeRefNode : IExpressionNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
    public IExpressionNode Value { get; set; }

    public DeRefNode(IExpressionNode value) => Value = value;

    public string GetSource()
    {
        throw new NotImplementedException();
    }
}
