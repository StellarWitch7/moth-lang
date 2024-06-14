namespace Moth.AST.Node;

public class ScopeNode : IStatementNode
{
    public List<IStatementNode> Statements { get; set; }

    public ScopeNode(List<IStatementNode> statements) => Statements = statements;

    public string GetSource()
    {
        var builder = new StringBuilder("{");

        foreach (IStatementNode statement in Statements)
        {
            string s = $"\n{statement.GetSource()}";

            if (builder.Length <= 1 && statement is IfNode or WhileNode)
                s = s.Remove(0, 1);

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
        }

        return $"{builder.ToString().TrimEnd()}\n}}";
    }
}
