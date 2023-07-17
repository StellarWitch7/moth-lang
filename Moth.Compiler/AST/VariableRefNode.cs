namespace Moth.Compiler.AST;

public class VariableRefNode : RefNode
{
	public string Name { get; }
	public RefNode Parent { get; }

	public VariableRefNode(string name, RefNode parent)
    {
		Name = name;
		Parent = parent;
	}
}