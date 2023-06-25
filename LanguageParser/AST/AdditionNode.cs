namespace LanguageParser.AST;

internal class AdditionNode : BinaryOperationNode
{
	public AdditionNode(ExpressionNode left, ExpressionNode right) : base(left, right) {}
}