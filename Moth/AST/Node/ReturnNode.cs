namespace Moth.AST.Node;

public class ReturnNode : SingleExprNode
{
    public ReturnNode(IExpressionNode? returnValue)
        : base(returnValue) { }

    public IExpressionNode? Expression
    {
        get => base.Expression;
        set => base.Expression = value;
    }

    public override string GetSource()
    {
        if (Expression == null)
            return Reserved.Return;

        return $"{Reserved.Return} {Expression.GetSource()}";
    }
}
