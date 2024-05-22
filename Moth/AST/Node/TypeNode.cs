namespace Moth.AST.Node;

public class TypeNode : DefinitionNode
{
    public string Name { get; set; }
    public PrivacyType Privacy { get; set; }
    public ScopeNode? Scope { get; set; }

    public TypeNode(string name, PrivacyType privacy, ScopeNode? scope, List<AttributeNode> attributes) : base(attributes)
    {
        Name = name;
        Privacy = privacy;
        Scope = scope;
    }

    public override string GetSource()
    {
        var builder = new StringBuilder();

        if (Privacy != PrivacyType.Priv)
            builder.Append($"{Privacy} ".ToLower());

        builder.Append($"type {Name}");

        if (Scope != default)
            builder.Append($" {Scope.GetSource()}");
        else
            builder.Append(";");
        
        return builder.ToString();
    }
}