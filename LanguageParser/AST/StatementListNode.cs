namespace LanguageParser.AST;

internal class StatementListNode : ASTNode
{
	public List<StatementNode> StatementNodes { get; }

	public StatementListNode(List<StatementNode> statements)
	{
		StatementNodes = statements;
	}
}