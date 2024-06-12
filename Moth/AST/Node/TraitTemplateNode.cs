namespace Moth.AST.Node;

public class TraitTemplateNode : TraitNode
{
    public TraitTemplateNode(
        string name,
        PrivacyType privacy,
        ScopeNode scope,
        List<AttributeNode> attributes
    )
        : base(name, privacy, scope, attributes)
    {
        throw new NotImplementedException(); //TODO
    }
}
