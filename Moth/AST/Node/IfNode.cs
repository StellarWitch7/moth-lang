namespace Moth.AST.Node;

public class IfNode : IStatementNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
    public IExpressionNode Condition { get; set; }
    public ScopeNode Then { get; set; }
    public ScopeNode? Else { get; set; }

    public IfNode(IExpressionNode condition, ScopeNode then, ScopeNode? @else)
    {
        Condition = condition;
        Then = then;
        Else = @else;
    }

    public string GetSource()
    {
        string s = $"\n{Reserved.If} {Condition.GetSource()} {Then.GetSource()}";

        if (Else == null)
            return $"{s}\n";

        return $"{s} else {Else.GetSource()}\n";
    }
}
