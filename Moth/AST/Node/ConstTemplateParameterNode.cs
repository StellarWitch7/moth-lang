namespace Moth.AST.Node;

public class ConstTemplateParameterNode : TemplateParameterNode
{
    public ITypeRefNode TypeRef { get; set; }

    public ConstTemplateParameterNode(string name, ITypeRefNode typeRef)
        : base(name) => TypeRef = typeRef;
}
