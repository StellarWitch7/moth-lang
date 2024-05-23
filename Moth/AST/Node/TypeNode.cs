using Moth.LLVM;

namespace Moth.AST.Node;

public class TypeNode : DefinitionNode
{
    public string Name { get; set; }
    public PrivacyType Privacy { get; set; }
    public ScopeNode? Scope { get; set; }
    public bool IsUnion { get; set; }

    public TypeNode(string name, PrivacyType privacy, ScopeNode? scope, bool isUnion, List<AttributeNode> attributes) : base(attributes)
    {
        Name = name;
        Privacy = privacy;
        Scope = scope;
        IsUnion = isUnion;
    }

    public override string GetSource()
    {
        var builder = new StringBuilder();

        if (Privacy != PrivacyType.Priv)
            builder.Append($"{Privacy} ".ToLower());

        if (IsUnion)
            builder.Append($"{Reserved.Union} ");

        builder.Append($"{Reserved.Type} {Name}");

        if (Scope != default)
            builder.Append($" {Scope.GetSource()}");
        else
            builder.Append(";");
        
        return builder.ToString();
    }
}