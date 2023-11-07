namespace Moth.AST.Node;

public class IncrementVarNode : ExpressionNode
{
    public RefNode RefNode { get; set; }

    public IncrementVarNode(RefNode refNode)
    {
        RefNode = refNode;
    }
}
