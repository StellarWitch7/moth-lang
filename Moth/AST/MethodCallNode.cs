namespace Moth.AST;

public class MethodCallNode : RefNode
{
	public List<ExpressionNode> Arguments { get; }
	public RefNode Parent { get; }

	public MethodCallNode(string name, List<ExpressionNode> arguments, RefNode parent) : base(name)
	{
		Arguments = arguments;
		Parent = parent;
	}
}