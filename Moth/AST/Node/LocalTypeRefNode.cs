namespace Moth.AST.Node;

public class LocalTypeRefNode : TypeRefNode
{
    public LocalTypeRefNode(string name, uint pointerDepth, bool isRef)
        : base(name, pointerDepth, isRef) { }

    public override string GetSource()
    {
        return base.GetSource().Remove(0, 1).Insert(0, "?");
    }
}
