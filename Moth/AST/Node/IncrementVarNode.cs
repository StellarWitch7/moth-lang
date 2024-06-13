namespace Moth.AST.Node;

public class IncrementVarNode : SingleExprNode
{
    public IncrementVarNode(IExpressionNode expr)
        : base(expr) { }

    public override string GetSource()
    {
        return $"++{Expression.GetSource()}";
    }
}
