namespace Moth.AST.Node;

public class ParameterNode : IASTNode
{
    public string Name { get; set; }
    public TypeRefNode TypeRef { get; set; }

    public ParameterNode(string name, TypeRefNode typeRef)
    {
        Name = name;
        TypeRef = typeRef;
    }

    public string GetSource()
    {
        return $"{Name} {TypeRef.GetSource()}";
    }
}
