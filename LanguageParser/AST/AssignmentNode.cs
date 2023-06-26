namespace LanguageParser.AST;

internal class AssignmentNode : StatementNode
{
	public VariableRefNode Left { get; }
	public ExpressionNode Right { get; }

	public AssignmentNode(VariableRefNode left, ExpressionNode right)
	{
		Left = left;
		Right = right;
	}
}