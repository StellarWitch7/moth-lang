namespace Moth.AST.Node;

public class PointerOfNode : ExpressionNode
{
    public ExpressionNode Value { get; set; }

    public PointerOfNode(ExpressionNode value) => Value = value;
}
