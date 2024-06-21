namespace Moth.AST.Node;

public class TraitNode : DefinitionNode
{
    public ScopeNode Scope { get; set; }

    public TraitNode(
        string name,
        PrivacyType privacy,
        ScopeNode scope,
        List<AttributeNode>? attributes
    )
        : base(name, privacy, attributes)
    {
        Scope = scope;
    }

    public override void GetSource(StringBuilder builder)
    {
        throw new NotImplementedException();
    }
}
