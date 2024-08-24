namespace Moth.AST.Node;

public class NamespaceNode : IExpressionNode
{
    public string Name { get; set; }
    public NamespaceNode? Child { get; set; }

    public NamespaceNode(string name)
    {
        Name = name;
    }

    public NamespaceNode(string name, NamespaceNode child)
        : this(name)
    {
        Child = child;
    }

    public string GetSource()
    {
        if (Child == default)
            return $"{Name}";

        return $"{Name}::{Child.GetSource()}";
    }
}
