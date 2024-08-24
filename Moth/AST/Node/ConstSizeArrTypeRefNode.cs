namespace Moth.AST.Node;

public class ConstSizeArrTypeRefNode : ArrTypeRefNode
{
    public long Size { get; set; }

    public ConstSizeArrTypeRefNode(ITypeRefNode elementType, long size)
        : base(elementType)
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
