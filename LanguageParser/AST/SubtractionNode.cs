namespace LanguageParser.AST;

internal class SubtractionNode : BinaryOperationNode
{
	public SubtractionNode(ExpressionNode left, ExpressionNode right) : base(left, right)
	{
	}
}