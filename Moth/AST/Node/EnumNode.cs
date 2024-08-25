namespace Moth.AST.Node;

public class EnumNode : DefinitionNode, ITopDeclNode
{
    public List<EnumFlagNode> EnumFlags { get; set; }
    public ScopeNode? Scope { get; set; } //TODO: don't use, should be private

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

    public override void GetSource(StringBuilder builder)
    {
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

        builder.Append("\n");
    }
}
