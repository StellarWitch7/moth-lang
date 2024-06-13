namespace Moth.AST.Node;

public class InlineIfNode : IExpressionNode
{
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
