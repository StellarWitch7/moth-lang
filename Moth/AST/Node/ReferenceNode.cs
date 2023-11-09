namespace Moth.AST.Node;

public class AsReferenceNode : ExpressionNode
{
    public ExpressionNode Value { get; set; }

    public AsReferenceNode(ExpressionNode value) => Value = value;
}
