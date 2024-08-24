namespace Moth.AST.Node;

public class WhileNode : IExpressionNode
{
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
