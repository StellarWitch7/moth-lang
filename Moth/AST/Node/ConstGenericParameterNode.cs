namespace Moth.AST.Node;

public class ConstGenericParameterNode : GenericParameterNode
{
    public TypeRefNode TypeRef { get; set; }

    public ConstGenericParameterNode(string name, TypeRefNode typeRef) : base(name) => TypeRef = typeRef;
}
