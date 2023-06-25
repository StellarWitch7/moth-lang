namespace LanguageParser.AST;

internal class VariableNode : ExpressionNode
{
	public string Name { get; }

	public VariableNode(string name)
	{
		Name = name;
	}
}