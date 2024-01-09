namespace Moth.AST.Node;

public class CastNode : ExpressionNode
{
    public TypeRefNode NewType { get; set; }
    public ExpressionNode Value { get; set; }

    public CastNode(TypeRefNode newType, ExpressionNode value)
    {
        NewType = newType;
        Value = value;
    }
}
