using Moth.LLVM;

namespace Moth.AST.Node;

public class EnumNode : DefinitionNode
{
    public List<EnumFlagNode> EnumFlags { get; set; }
    public ScopeNode? Scope { get; set; } //TODO: don't use this

    public EnumNode(
        string name,
        PrivacyType privacy,
        List<EnumFlagNode> enumFlags,
        ScopeNode? scope,
        List<AttributeNode>? attributes
    )
        : base(name, privacy, attributes)
    {
        EnumFlags = enumFlags;
        Scope = scope;
    }

    public override string GetSource()
    {
        var builder = new StringBuilder();

        builder.Append(GetAttributeSource());

        if (Privacy != PrivacyType.Priv)
            builder.Append($"{Privacy} ".ToLower());

        builder.Append($"{Reserved.Enum} {Name} {{\n");

        foreach (var flag in EnumFlags)
        {
            builder.Append($"    {flag.GetSource()},\n");
        }

        builder.Append("}");

        if (Scope != null)
            builder.Append($" {Reserved.Extend} {Scope.GetSource()}");

        return builder.ToString();
    }
}
