namespace LanguageParser.AST;

internal class VariableRefNode : ExpressionNode
{
	public string Name { get; }

	public VariableRefNode(string name)
	{
		Name = name;
	}
}