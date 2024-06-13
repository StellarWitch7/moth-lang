namespace Moth.AST.Node;

public class TraitNode : IDefinitionNode
{
    public string Name { get; set; }
    public PrivacyType Privacy { get; set; }
    public ScopeNode Scope { get; set; }
    public List<AttributeNode> Attributes { get; set; }

    public TraitNode(
        string name,
        PrivacyType privacy,
        ScopeNode scope,
        List<AttributeNode> attributes
    )
    {
        Name = name;
        Privacy = privacy;
        Scope = scope;
        Attributes = attributes;
    }

    public string GetSource()
    {
        throw new NotImplementedException();
    }
}
