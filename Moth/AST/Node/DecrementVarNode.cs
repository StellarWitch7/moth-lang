namespace Moth.AST.Node;

public class DecrementVarNode : SingleExprNode
{
    public DecrementVarNode(IExpressionNode expr)
        : base(expr) { }

    public override string GetSource()
    {
        return $"--{Expression.GetSource()}";
    }
}
