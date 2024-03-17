namespace Moth.AST.Node;

public class DecrementVarNode : ExpressionNode
{
    public ExpressionNode Value { get; set; }

    public DecrementVarNode(ExpressionNode value) => Value = value;
}
