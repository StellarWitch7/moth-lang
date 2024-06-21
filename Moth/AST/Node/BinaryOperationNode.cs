namespace Moth.AST.Node;

public class BinaryOperationNode : IExpressionNode
{
    public OperationType Type { get; set; }
    public IExpressionNode Left { get; set; }
    public IExpressionNode Right { get; set; } = null;

    public BinaryOperationNode(IExpressionNode left, OperationType type)
    {
        Left = left;
        Type = type;
    }

    public string GetSource()
    {
        return $"{Left.GetSource()} {Utils.OpTypeToString(Type)} {Right.GetSource()}";
    }
}

public enum OperationType
{
    Addition,
    Subtraction,
    Multiplication,
    Division,
    Exponential,
    LesserThan,
    LesserThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    Equal,
    NotEqual,
    Modulus,
    Assignment,
    Or,
    And,
}
