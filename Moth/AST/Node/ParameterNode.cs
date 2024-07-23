namespace Moth.AST.Node;

public class ParameterNode : IASTNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
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
