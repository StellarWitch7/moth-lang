namespace Moth.AST.Node;

public class TypeRefNode : ExpressionNode
{
    public string Name { get; set; }
    public uint PointerDepth { get; set; }
    public bool IsRef { get; set; }
    public NamespaceNode Namespace { get; set; }

    public TypeRefNode(string name, uint pointerDepth, bool isRef)
    {
        Name = name;
        PointerDepth = pointerDepth;
        IsRef = isRef;
    }
}
