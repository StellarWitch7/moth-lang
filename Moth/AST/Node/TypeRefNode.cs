namespace Moth.AST.Node;

public class TypeRefNode : RefNode
{
    public uint PointerDepth { get; set; }

    public TypeRefNode(string name, uint pointerDepth = 0) : base(name)
    {
        PointerDepth = pointerDepth;
    }
}
