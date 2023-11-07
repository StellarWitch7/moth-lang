namespace Moth.AST.Node;

public class RefNode : ExpressionNode
{
    public string Name { get; set; }
    public RefNode Child { set; get; }

    public RefNode(string name)
    {
        Name = name;
        Child = null;
    }
}
