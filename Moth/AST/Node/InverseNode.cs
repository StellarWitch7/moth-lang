namespace Moth.AST.Node;

public class InverseNode : ExpressionNode
{
    public RefNode Value { get; set; }

    public InverseNode(RefNode value)
    {
        Value = value;
    }
}
