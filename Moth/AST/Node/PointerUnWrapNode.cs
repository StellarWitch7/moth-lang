namespace Moth.AST.Node;

public class DeRefNode : ExpressionNode
{
    public ExpressionNode Value { get; set; }

    public DeRefNode(ExpressionNode value) => Value = value;
}
