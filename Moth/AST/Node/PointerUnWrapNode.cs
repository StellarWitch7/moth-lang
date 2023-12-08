namespace Moth.AST.Node;

public class LoadNode : ExpressionNode
{
    public ExpressionNode Value { get; set; }

    public LoadNode(ExpressionNode value) => Value = value;
}
