namespace Moth.AST.Node;

public class LocalTypeRefNode : TypeRefNode
{
    public LocalTypeRefNode(string name, uint pointerDepth = 0) : base(name, pointerDepth) { }
}
