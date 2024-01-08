namespace Moth.AST.Node;

public class LiteralArrayNode : ExpressionNode
{
    public TypeRefNode ElementType { get; set; }
    public ExpressionNode[] Elements { get; set; }

    public LiteralArrayNode(TypeRefNode elementType, ExpressionNode[] elements)
    {
        ElementType = elementType;
        Elements = elements;
    }
}
