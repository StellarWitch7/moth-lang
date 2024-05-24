namespace Moth.AST.Node;

public class BinaryOperationNode : ExpressionNode
{
    public OperationType Type { get; set; }
    public ExpressionNode Left { get; set; }
    public ExpressionNode Right { get; set; } = null;

    public BinaryOperationNode(ExpressionNode left, OperationType type)
    {
        Left = left;
        Type = type;
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
