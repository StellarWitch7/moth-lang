namespace Moth.AST.Node;

public class LiteralArrayNode : IExpressionNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
    public TypeRefNode ElementType { get; set; }
    public IExpressionNode[] Elements { get; set; }

    public LiteralArrayNode(TypeRefNode elementType, IExpressionNode[] elements)
    {
        ElementType = elementType;
        Elements = elements;
    }

    public string GetSource()
    {
        throw new NotImplementedException();
    }
}
