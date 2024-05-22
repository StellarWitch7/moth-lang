namespace Moth.AST.Node;

public class ScopeNode : StatementNode
{
    public List<StatementNode> Statements { get; set; }

    public ScopeNode(List<StatementNode> statements) => Statements = statements;

    public override string GetSource()
    {
        var builder = new StringBuilder("{");

        foreach (StatementNode statement in Statements)
        {
            string s = $"\n{statement.GetSource()}";
            builder.Append(s.Replace("\n", "\n    "));
        }
        
        builder.Append("\n}");
        return builder.ToString();
    }
}
