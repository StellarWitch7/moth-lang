namespace LanguageParser.AST;

internal class CallNode : StatementNode
{
	public MethodNode Method { get; }

	public CallNode(MethodNode method)
	{
		Method = method;
	}
}