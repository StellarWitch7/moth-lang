namespace Moth.AST;

public class StatementListNode : StatementNode
{
	public List<StatementNode> StatementNodes { get; }

	public StatementListNode(List<StatementNode> statements)
	{
		StatementNodes = statements;
	}
}