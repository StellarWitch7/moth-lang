namespace Moth.AST.Node;

public class IndexAccessNode : ExpressionNode
{
    public ExpressionNode ToBeIndexed { get; set; }
    public IReadOnlyList<ExpressionNode> Params { get; set; }

    public IndexAccessNode(ExpressionNode toBeIndexed, IReadOnlyList<ExpressionNode> @params)
    {
        ToBeIndexed = toBeIndexed;
        Params = @params;
    }
}
