namespace Moth.AST.Node;

public class CastNode : IExpressionNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
    public TypeRefNode NewType { get; set; }
    public SubExprNode Value { get; set; }

    public CastNode(TypeRefNode newType, SubExprNode value)
    {
        NewType = newType;
        Value = value;
    }

    public string GetSource()
    {
        return $"{NewType.GetSource()}{Value.GetSource()}";
    }
}
