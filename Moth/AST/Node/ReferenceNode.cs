namespace Moth.AST.Node;

public class AddressOfNode : ExpressionNode
{
    public ExpressionNode Value { get; set; }

    public AddressOfNode(ExpressionNode value) => Value = value;
}
