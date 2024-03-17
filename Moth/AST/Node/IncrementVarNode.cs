namespace Moth.AST.Node;

public class IncrementVarNode : ExpressionNode
{
    public ExpressionNode Value { get; set; }

    public IncrementVarNode(ExpressionNode value) => Value = value;
}
