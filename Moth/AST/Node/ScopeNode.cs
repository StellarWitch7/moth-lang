namespace Moth.AST.Node;

public class ScopeNode : IExpressionNode
{
    public List<IExpressionNode> Statements { get; set; }

    public ScopeNode(List<IExpressionNode> statements) => Statements = statements;

    public string GetSource()
    {
        var builder = new StringBuilder("{");
        IExpressionNode last = null;

        foreach (IExpressionNode statement in Statements)
        {
            string s = $"\n{statement.GetSource()}";

            if (
                (last is null && statement is IfNode or WhileNode or LocalDefNode)
                || (last is not null && statement is FieldDefNode)
                || (last is LocalDefNode && statement is LocalDefNode)
            )
                s = s.Remove(0, 1);

            s = s.Replace("\n", "\n    ");

            if (statement is ReturnNode or ScopeNode or DefinitionNode or IfNode or WhileNode)
                builder.Append(s);
            else
                builder.Append($"{s};");

            last = statement;
        }

        return $"{builder.ToString().TrimEnd()}\n}}";
    }
}
