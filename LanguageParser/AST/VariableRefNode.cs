namespace LanguageParser.AST;

public class VariableRefNode : RefNode
{
	public RefNode Origin { get; }
	public string Name { get; }

	public VariableRefNode(string name, RefNode classRef)
	{
		Name = name;
		Origin = classRef;
	}
}