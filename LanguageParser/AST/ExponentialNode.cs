namespace LanguageParser.AST;

internal class ExponentialNode : BinaryOperationNode
{
	public ExponentialNode(ExpressionNode left, ExpressionNode right) : base(left, right)
	{
	}
}