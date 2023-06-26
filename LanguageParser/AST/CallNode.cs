namespace LanguageParser.AST;

internal class CallNode : StatementNode
{
	public MethodCallNode Method { get; }

	public CallNode(MethodCallNode method)
	{
		Method = method;
	}
}