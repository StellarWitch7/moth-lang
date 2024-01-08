namespace Moth.AST.Node;

public class ArrayTypeRefNode : TypeRefNode
{
    public TypeRefNode ElementType { get; set; }

    public ArrayTypeRefNode(TypeRefNode elementType, uint pointerDepth = 0) : base(null, pointerDepth)
    {
        ElementType = elementType;
    }
}
