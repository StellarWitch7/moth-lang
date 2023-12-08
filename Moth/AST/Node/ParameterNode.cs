namespace Moth.AST.Node;

public class ParameterNode : ASTNode
{
    public string Name { get; set; }
    public TypeRefNode TypeRef { get; set; }

    public ParameterNode(string name, TypeRefNode typeRef)
    {
        Name = name;
        TypeRef = typeRef;
    }
}