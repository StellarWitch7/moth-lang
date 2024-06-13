using Moth.LLVM;

namespace Moth.AST.Node;

public class TypeNode : IDefinitionNode
{
    public string Name { get; set; }
    public PrivacyType Privacy { get; set; }
    public ScopeNode? Scope { get; set; }
    public bool IsUnion { get; set; }
    public List<AttributeNode> Attributes { get; set; }

    public TypeNode(
        string name,
        PrivacyType privacy,
        ScopeNode? scope,
        bool isUnion,
        List<AttributeNode> attributes
    )
    {
        Name = name;
        Privacy = privacy;
        Scope = scope;
        IsUnion = isUnion;
        Attributes = attributes;
    }

    public virtual string GetSource()
    {
        var builder = new StringBuilder("\n");

        if (Privacy != PrivacyType.Priv)
            builder.Append($"{Privacy} ".ToLower());

        if (IsUnion)
            builder.Append($"{Reserved.Union} ");

        builder.Append($"{Reserved.Type} {Name}");

        if (Scope != default)
            builder.Append($" {Scope.GetSource()}\n");
        else
            builder.Append(";");

        return builder.ToString();
    }
}
