namespace Moth.AST;

public class VariableRefNode : RefNode
{
	public string Name { get; }
	public RefNode Parent { get; }
	public bool IsLocalVar { get; }

	public VariableRefNode(string name, RefNode parent, bool isLocal = false)
    {
		Name = name;
		Parent = parent;
		IsLocalVar = isLocal;
	}
}