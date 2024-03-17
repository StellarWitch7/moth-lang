namespace Moth.AST.Node;

public class RefNode : ExpressionNode
{
    public string Name { get; set; }
    public ExpressionNode? Parent { set; get; }

    public RefNode(string name, ExpressionNode parent)
    {
        Name = name;
        Parent = parent;
    }
}
