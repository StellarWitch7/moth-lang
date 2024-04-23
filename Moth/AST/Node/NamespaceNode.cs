namespace Moth.AST.Node;

public class NamespaceNode : ExpressionNode
{
    public string Name { get; set; }
    public NamespaceNode Child { get; set; }

    public NamespaceNode(string name)
    {
        Name = name;
    }
}