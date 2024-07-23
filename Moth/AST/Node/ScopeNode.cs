namespace Moth.AST.Node;

public class ScopeNode : IStatementNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
    public List<IStatementNode> Statements { get; set; }

    public ScopeNode(List<IStatementNode> statements) => Statements = statements;

    public string GetSource()
    {
        var builder = new StringBuilder("{");
        IStatementNode last = null;

        foreach (IStatementNode statement in Statements)
        {
            string s = $"\n{statement.GetSource()}";

            if (
                (last is null && statement is IfNode or WhileNode or LocalDefNode)
                || (last is not null && statement is FieldDefNode)
                || (last is LocalDefNode or CommentNode && statement is LocalDefNode)
            )
                s = s.Remove(0, 1);

            if (last is not null or CommentNode && statement is CommentNode)
                s = s.Insert(0, "\n");

            s = s.Replace("\n", "\n    ");

            if (
                statement
                is CommentNode
                    or ReturnNode
                    or ScopeNode
                    or DefinitionNode
                    or IfNode
                    or WhileNode
            )
                builder.Append(s);
            else
                builder.Append($"{s};");

            last = statement;
        }

        return $"{builder.ToString().TrimEnd()}\n}}";
    }
}
