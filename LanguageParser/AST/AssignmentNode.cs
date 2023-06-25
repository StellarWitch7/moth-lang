namespace LanguageParser.AST;

internal class AssignmentNode : StatementNode
{
	public VariableNode Left { get; }
	public ExpressionNode Right { get; }

	public AssignmentNode(VariableNode left, ExpressionNode right)
	{
		Left = left;
		Right = right;
	}
}