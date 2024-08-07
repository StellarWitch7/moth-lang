namespace Moth.AST.Node;

public class CastNode : IExpressionNode
{
    public TypeRefNode NewType { get; set; }
    public SubExprNode Value { get; set; }

    public CastNode(TypeRefNode newType, SubExprNode value)
    {
        NewType = newType;
        Value = value;
    }

    public string GetSource()
    {
        return $"{NewType.GetSource()}{Value.GetSource()}";
    }
}
