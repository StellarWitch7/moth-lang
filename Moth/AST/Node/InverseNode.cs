namespace Moth.AST.Node;

public class InverseNode : SingleExprNode
{
    public InverseNode(IExpressionNode expr)
        : base(expr) { }

    public override string GetSource()
    {
        return $"!{Expression}";
    }
}
