namespace Moth.AST.Node;

public class ClassNode : DefinitionNode
{
    public string Name { get; set; }
    public PrivacyType Privacy { get; set; }
    public ScopeNode? Scope { get; set; }
    public bool IsStruct { get; set; }

    public ClassNode(string name, PrivacyType privacy, ScopeNode? scope, bool isStruct) : base(new List<AttributeNode>())
    {
        Name = name;
        Privacy = privacy;
        Scope = scope;
        IsStruct = isStruct;
    }
}