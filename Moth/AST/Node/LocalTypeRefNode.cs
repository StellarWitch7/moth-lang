namespace Moth.AST.Node;

public class LocalTypeRefNode : NamedTypeRefNode
{
    public LocalTypeRefNode(string name)
        : base(name) { }

    public override string GetSource()
    {
        return base.GetSource().Remove(0, 1).Insert(0, "?");
    }
}
