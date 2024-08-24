using Moth.LLVM;

namespace Moth.AST.Node;

public class TypeNode : MemberContainingDefinitionNode, ITopDeclNode
{
    public bool IsUnion { get; set; }

    public TypeNode(
        string name,
        PrivacyType privacy,
        List<IMemberDeclNode>? contents,
        bool isUnion,
        List<AttributeNode>? attributes
    )
        : base(name, privacy, contents, attributes)
    {
        IsUnion = isUnion;
    }

    public bool IsOpaque
    {
        get => IsEmpty;
    }

    public override void GetSource(StringBuilder builder)
    {
        if (Privacy != PrivacyType.Priv)
            builder.Append($"{Privacy} ".ToLower());

        if (IsUnion)
            builder.Append($"{Reserved.Union} ");

        builder.Append($"{Reserved.Type} {Name}");

        if (!IsOpaque)
            builder.Append(
                $"{{\n{String.Join("\n", _contents.ToArray().ExecuteOverAll(e => {
                return e.GetSource();
            })).ReplaceLineEndings("    \n")}\n}}"
            );
        else
            builder.Append(";");
    }
}
