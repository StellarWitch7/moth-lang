namespace Moth.AST.Node;

public class LocalDefNode : IExpressionNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
    public string Name { get; set; }
    public TypeRefNode TypeRef { get; set; }

    public LocalDefNode(string name, TypeRefNode typeRef)
    {
        Name = name;
        TypeRef = typeRef;
    }

    public virtual string GetSource()
    {
        return $"\n{Reserved.Var} {Name} {TypeRef.GetSource()}";
    }
}
