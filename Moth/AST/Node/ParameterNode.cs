namespace Moth.AST.Node;

public class ParameterNode : IASTNode
{
    public string Name { get; set; }
    public ITypeRefNode TypeRef { get; set; }

    public ParameterNode(string name, ITypeRefNode typeRef)
    {
        Name = name;
        TypeRef = typeRef;
    }

    public string GetSource()
    {
        return $"{Name} {TypeRef.GetSource()}";
    }
}
