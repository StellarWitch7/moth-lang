namespace Moth.AST.Node;

public class WhileNode : IStatementNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
    public IExpressionNode Condition { get; set; }
    public ScopeNode Then { get; set; }

    public WhileNode(IExpressionNode condition, ScopeNode then)
    {
        Condition = condition;
        Then = then;
    }

    public string GetSource()
    {
        return $"{Reserved.While} {Condition.GetSource()} {Then.GetSource()}\n";
    }
}
