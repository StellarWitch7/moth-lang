namespace Moth.AST.Node;

public class ScopeNode : IStatementNode
{
    public List<IStatementNode> Statements { get; set; }

    public ScopeNode(List<IStatementNode> statements) => Statements = statements;

    public string GetSource()
    {
        var builder = new StringBuilder("{");
        IStatementNode last = null;

        foreach (IStatementNode statement in Statements)
        {
            string s = $"\n{statement.GetSource()}";

            if (builder.Length <= 1 && statement is IfNode or WhileNode)
                s = s.Remove(0, 1);

            if (statement is FuncDefNode && (last is FuncDefNode || builder.Length <= 1))
                s = s.Remove(0, 1);

            s = s.Replace("\n", "\n    ");

            if (statement is ReturnNode or ScopeNode or IDefinitionNode or IfNode or WhileNode)
                builder.Append(s);
            else
                builder.Append($"{s};");

            last = statement;
        }

        return $"{builder.ToString().TrimEnd()}\n}}";
    }
}
