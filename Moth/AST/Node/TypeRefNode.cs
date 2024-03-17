namespace Moth.AST.Node;

public class TypeRefNode : ExpressionNode
{
    public string Name { get; set; }
    public uint PointerDepth { get; set; }

    public TypeRefNode(string name, uint pointerDepth = 0)
    {
        Name = name;
        PointerDepth = pointerDepth;
    }
}
