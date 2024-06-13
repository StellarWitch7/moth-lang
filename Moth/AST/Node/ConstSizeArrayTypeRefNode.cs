namespace Moth.AST.Node;

public class ConstSizeArrayTypeRefNode : ArrayTypeRefNode
{
    public long Size { get; set; }

    public ConstSizeArrayTypeRefNode(
        TypeRefNode elementType,
        uint pointerDepth,
        bool isRef,
        long size
    )
        : base(elementType, pointerDepth, isRef)
    {
        if (size < 0)
            throw new ArgumentOutOfRangeException("size", size, "Value is negative.");

        Size = size;
    }

    public string GetSource()
    {
        return $"#[{ElementType.GetSource()}; {Size}]";
    }
}
