using Moth.LLVM;

namespace Moth.AST.Node;

public class EnumNode : DefinitionNode
{
    public string Name { get; set; }
    public PrivacyType Privacy { get; set; }
    public List<EnumFlagNode> EnumFlags { get; set; }
    public ScopeNode? Scope { get; set; }
    
    public EnumNode(string name, PrivacyType privacy, List<EnumFlagNode> enumFlags, ScopeNode? scope, List<AttributeNode>? attributes) : base(attributes)
    {
        Name = name;
        Privacy = privacy;
        EnumFlags = enumFlags;
        Scope = scope;
    }

    public override string GetSource()
    {
        var builder = new StringBuilder();

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
