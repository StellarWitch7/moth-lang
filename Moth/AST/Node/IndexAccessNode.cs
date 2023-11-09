namespace Moth.AST.Node;

public class IndexAccessNode : RefNode
{
    public ExpressionNode Index { get; set; }

    public IndexAccessNode(string name, ExpressionNode index) : base(name) => Index = index;
}
