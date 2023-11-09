namespace Moth.AST.Node;

public class LocalDefNode : ExpressionNode
{
    public string Name { get; set; }
    public TypeRefNode TypeRef { get; set; }

    public LocalDefNode(string name, TypeRefNode typeRef)
    {
        Name = name;
        TypeRef = typeRef;
    }
}
