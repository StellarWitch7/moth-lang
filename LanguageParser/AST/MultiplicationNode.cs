namespace LanguageParser.AST;

internal class MultiplicationNode : BinaryOperationNode
{
	public MultiplicationNode(ExpressionNode left, ExpressionNode right) : base(left, right)
	{
	}
}