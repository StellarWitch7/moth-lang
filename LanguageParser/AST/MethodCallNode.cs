namespace LanguageParser.AST;

internal class MethodCallNode : StatementNode
{
	public string Name { get; }
	public string OriginClass { get; }
	public List<ExpressionNode> Arguments { get; }

	public MethodCallNode(string name, string originClass, List<ExpressionNode> arguments)
	{
		Name = name;
		OriginClass = originClass;
		Arguments = arguments;
	}
}