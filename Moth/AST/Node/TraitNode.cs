namespace Moth.AST.Node;

public class TraitNode : DefinitionNode
{
    public string Name { get; set; }
    public PrivacyType Privacy { get; set; }
    public ScopeNode Scope { get; set; }

    public TraitNode(string name, PrivacyType privacy, ScopeNode scope, List<AttributeNode> attributes) : base(attributes)
    {
        Name = name;
        Privacy = privacy;
        Scope = scope;
    }
}
