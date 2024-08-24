namespace Moth.AST.Node;

public class LiteralArrayNode : IExpressionNode
{
    public ITypeRefNode ElementType { get; set; }
    public IExpressionNode[] Elements { get; set; }

    public LiteralArrayNode(ITypeRefNode elementType, IExpressionNode[] elements)
    {
        ElementType = elementType;
        Elements = elements;
    }

    public string GetSource()
    {
        throw new NotImplementedException();
    }
}
