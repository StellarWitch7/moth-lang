namespace Moth.AST.Node;

public class InlineIfNode : ExpressionNode
{
    public ExpressionNode Condition { get; set; }
    public ExpressionNode Then { get; set; }
    public ExpressionNode Else { get; set; }

    public InlineIfNode(ExpressionNode condition, ExpressionNode then, ExpressionNode @else)
    {
        Condition = condition;
        Then = then;
        Else = @else;
    }
}
