using Moth.LLVM;

namespace Moth.AST.Node;

public class TypeNode : DefinitionNode
{
    public bool IsUnion { get; set; }
    public ScopeNode? Contents { get; set; }

    public TypeNode(
        string name,
        PrivacyType privacy,
        ScopeNode? contents,
        bool isUnion,
        List<AttributeNode>? attributes
    )
        : base(name, privacy, attributes)
    {
        IsUnion = isUnion;
        Contents = contents;
    }

    public bool IsOpaque
    {
        get => Contents == null;
    }

    public FieldDefNode[] Fields
    {
        get
        {
            return IsOpaque
                ? new FieldDefNode[0]
                : Contents.Statements.OfType<FieldDefNode>().ToArray();
        }
    }

    public FuncDefNode[] Functions
    {
        get
        {
            return IsOpaque
                ? new FuncDefNode[0]
                : Contents.Statements.OfType<FuncDefNode>().ToArray();
        }
    }

    public DefinitionNode[] OrganizedMembers
    {
        get
        {
            var result = new List<DefinitionNode>();

            result.AddRange(Fields);
            result.AddRange(Functions);

            return result.ToArray();
        }
    }

    public override void GetSource(StringBuilder builder)
    {
        if (Privacy != PrivacyType.Priv)
            builder.Append($"{Privacy} ".ToLower());

        if (IsUnion)
            builder.Append($"{Reserved.Union} ");

        builder.Append($"{Reserved.Type} {Name}");

        if (!IsOpaque)
            builder.Append($" {Contents.GetSource()}");
        else
            builder.Append(";");
    }
}
