namespace Moth.AST.Node;

public class LocalDefNode : IExpressionNode
{
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
