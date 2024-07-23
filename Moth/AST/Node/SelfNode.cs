namespace Moth.AST.Node;

public class SelfNode : IExpressionNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }

    public string GetSource()
    {
        return Reserved.Self;
    }
}
