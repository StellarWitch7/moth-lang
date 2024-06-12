namespace Moth.AST.Node;

public class TypeTemplateNode : TypeNode
{
    public List<TemplateParameterNode> Params { get; set; }

    public TypeTemplateNode(
        string name,
        PrivacyType privacy,
        List<TemplateParameterNode> @params,
        ScopeNode scope,
        bool isUnion,
        List<AttributeNode> attributes
    )
        : base(name, privacy, scope, isUnion, attributes) => Params = @params;
}
