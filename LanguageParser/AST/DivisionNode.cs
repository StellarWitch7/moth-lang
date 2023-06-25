namespace LanguageParser.AST;

internal class DivisionNode : BinaryOperationNode
{
	public DivisionNode(ExpressionNode left, ExpressionNode right) : base(left, right)
	{
	}
}