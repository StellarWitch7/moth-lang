namespace Moth.AST.Node;

public class ConstTemplateParameterNode : TemplateParameterNode
{
    public TypeRefNode TypeRef { get; set; }

    public ConstTemplateParameterNode(string name, TypeRefNode typeRef) : base(name) => TypeRef = typeRef;
}
