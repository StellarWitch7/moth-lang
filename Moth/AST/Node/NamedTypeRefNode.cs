namespace Moth.AST.Node;

public class NamedTypeRefNode : ITypeRefNode
{
    public string Name { get; set; }

    public NamedTypeRefNode(string name)
    {
        Name = name;
    }

    public virtual string GetSource() => $"#{Name}";

    public string GetSource(bool asChild) => GetSource();
}
