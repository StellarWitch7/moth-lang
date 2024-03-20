namespace Moth.AST.Node;

public class RefOfNode : ExpressionNode
{
    public ExpressionNode Value { get; set; }

    public RefOfNode(ExpressionNode value) => Value = value;
}
