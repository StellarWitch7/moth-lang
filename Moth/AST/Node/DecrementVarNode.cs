namespace Moth.AST.Node;

public class DecrementVarNode : ExpressionNode
{
    public RefNode RefNode { get; set; }

    public DecrementVarNode(RefNode refNode) => RefNode = refNode;
}
