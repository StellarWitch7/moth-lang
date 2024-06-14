using Moth.LLVM;

namespace Moth.AST.Node;

public class GlobalVarNode : DefinitionNode
{
    public TypeRefNode TypeRef { get; set; }
    public bool IsConstant { get; set; }
    public bool IsForeign { get; set; }

    public GlobalVarNode(
        string name,
        TypeRefNode typeRef,
        PrivacyType privacy,
        bool isConstant,
        bool isForeign,
        List<AttributeNode>? attributes
    )
        : base(name, privacy, attributes)
    {
        TypeRef = typeRef;
        IsConstant = isConstant;
        IsForeign = isForeign;
    }

    public override string GetSource()
    {
        var builder = new StringBuilder("\n");

        builder.Append(GetAttributeSource());

        if (Privacy != PrivacyType.Priv)
            builder.Append($"{Privacy} ".ToLower());

        if (IsForeign)
            builder.Append($"{Reserved.Foreign} ");

        if (IsConstant)
            builder.Append($"{Reserved.Constant} ");

        builder.Append($"{Reserved.Global} {Name} ");
        builder.Append($"{TypeRef.GetSource()};");
        return builder.ToString();
    }
}
