namespace Moth.AST.Node;

public class PtrTypeRefNode : ITypeRefNode
{
    public ITypeRefNode Child { get; set; }

    public PtrTypeRefNode(ITypeRefNode child)
    {
        Child = child;
    }

    public virtual string GetSource() => $"{Child.GetSource(true)}*";

    public string GetSource(bool asChild) => GetSource();
}
