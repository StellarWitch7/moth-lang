namespace Moth.AST.Node;

public class ArrayTypeRefNode : TypeRefNode
{
    public TypeRefNode ElementType { get; set; }

    public ArrayTypeRefNode(TypeRefNode elementType, uint pointerDepth, bool isRef)
        : base(null, pointerDepth, isRef)
    {
        ElementType = elementType;
    }
}
