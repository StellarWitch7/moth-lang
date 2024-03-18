namespace Moth.AST.Node;

public class StructNode : DefinitionNode
{
    public string Name { get; set; }
    public PrivacyType Privacy { get; set; }
    public ScopeNode? Scope { get; set; }

    public StructNode(string name, PrivacyType privacy, ScopeNode? scope) : base(new List<AttributeNode>())
    {
        Name = name;
        Privacy = privacy;
        Scope = scope;
    }
}