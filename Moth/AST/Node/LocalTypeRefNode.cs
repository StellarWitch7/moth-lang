namespace Moth.AST.Node;

public class LocalTypeRefNode : TypeRefNode
{
    public LocalTypeRefNode(string name, uint pointerDepth, bool isRef)
        : base(name, pointerDepth, isRef) { }
}
