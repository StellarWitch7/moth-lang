namespace LanguageParser.AST;

public class MethodCallNode : RefNode
{
	public string Name { get; }
	public RefNode Origin { get; }
	public List<ExpressionNode> Arguments { get; }

	public MethodCallNode(string name, RefNode originClass, List<ExpressionNode> arguments)
	{
		Name = name;
		Origin = originClass;
		Arguments = arguments;
	}
}