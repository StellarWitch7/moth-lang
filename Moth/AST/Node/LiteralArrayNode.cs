namespace Moth.AST.Node;

public class LiteralArrayNode : IExpressionNode
{
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
