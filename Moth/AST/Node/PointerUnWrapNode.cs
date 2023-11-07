namespace Moth.AST.Node;

public class DeReferenceNode : ExpressionNode
{
    public ExpressionNode Value { get; set; }

    public DeReferenceNode(ExpressionNode value)
    {
        Value = value;
    }
}
