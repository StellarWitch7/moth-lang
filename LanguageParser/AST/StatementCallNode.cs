namespace LanguageParser.AST;

internal class StatementCallNode : StatementNode
{
	public RefNode Ref { get; }

	public StatementCallNode(RefNode @ref)
	{
		Ref = @ref;
	}
}