namespace Moth.Compiler.AST;

public class BinaryOperationNode : ExpressionNode
{
    public OperationType Type { get; }
    public ExpressionNode Left { get; set; }
	public ExpressionNode Right { get; set; }

	public BinaryOperationNode(ExpressionNode left, ExpressionNode right, OperationType type)
	{
		Left = left;
		Right = right;
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
    LessThan,
    LessThanOrEqual,
    LargerThan,
    LargerThanOrEqual,
    Equal,
    NotEqual,
    And,
    Or,
    Xor,
    Modulo,
    Assignment,
    LogicalOr,
    LogicalAnd,
    LogicalXor,
    LogicalNand
}