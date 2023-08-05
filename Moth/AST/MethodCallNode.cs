namespace Moth.AST;

public class MethodCallNode : RefNode
{
	public string Name { get; }
	public List<ExpressionNode> Arguments { get; }
	public RefNode Parent { get; }

	public MethodCallNode(string name, List<ExpressionNode> arguments, RefNode parent)
	{
		Name = name;
		Arguments = arguments;
		Parent = parent;
	}
}