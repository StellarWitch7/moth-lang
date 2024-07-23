namespace Moth.AST.Node;

public class InlineIfNode : IExpressionNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
    public IExpressionNode Condition { get; set; }
    public IExpressionNode Then { get; set; }
    public IExpressionNode Else { get; set; }

    public InlineIfNode(IExpressionNode condition, IExpressionNode then, IExpressionNode @else)
    {
        Condition = condition;
        Then = then;
        Else = @else;
    }

    public string GetSource()
    {
        throw new NotImplementedException();
    }
}
