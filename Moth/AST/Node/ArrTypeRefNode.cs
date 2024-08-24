namespace Moth.AST.Node;

public class ArrTypeRefNode : ITypeRefNode
{
    public ITypeRefNode ElementType { get; set; }

    public ArrTypeRefNode(ITypeRefNode elementType)
    {
        ElementType = elementType;
    }

    public string GetSource() => $"#[{ElementType.GetSource()}]";

    public string GetSource(bool asChild) => GetSource();
}
