namespace Moth.AST.Node;

public class InverseNode : ExpressionNode
{
    public ExpressionNode Value { get; set; }

    public InverseNode(ExpressionNode value) => Value = value;
}
