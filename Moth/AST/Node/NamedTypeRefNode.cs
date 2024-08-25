namespace Moth.AST.Node;

public class NamedTypeRefNode : ITypeRefNode
{
    public NamespaceNode? Namespace { get; set; }
    public string Name { get; set; }

    public NamedTypeRefNode(string name, NamespaceNode? nmspace)
    {
        Name = name;
        Namespace = nmspace;
    }

    public virtual string GetSource() => $"#{Name}";

    public string GetSource(bool asChild) => GetSource();
}
