using Moth.LLVM;

namespace Moth.AST.Node;

public class TypeNode : DefinitionNode
{
    public bool IsUnion { get; set; }

    private ScopeNode? _scope;

    public TypeNode(
        string name,
        PrivacyType privacy,
        ScopeNode? scope,
        bool isUnion,
        List<AttributeNode>? attributes
    )
        : base(name, privacy, attributes)
    {
        IsUnion = isUnion;
        _scope = scope;
    }

    public bool IsOpaque
    {
        get => _scope == null;
    }

    public FieldDefNode[] Fields
    {
        get
        {
            return IsOpaque
                ? new FieldDefNode[0]
                : _scope.Statements.OfType<FieldDefNode>().ToArray();
        }
    }

    public FuncDefNode[] Functions
    {
        get
        {
            return IsOpaque
                ? new FuncDefNode[0]
                : _scope.Statements.OfType<FuncDefNode>().ToArray();
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

    public override string GetSource()
    {
        var builder = new StringBuilder("\n");

        builder.Append(GetAttributeSource());

        if (Privacy != PrivacyType.Priv)
            builder.Append($"{Privacy} ".ToLower());

        if (IsUnion)
            builder.Append($"{Reserved.Union} ");

        builder.Append($"{Reserved.Type} {Name}");

        if (IsOpaque)
            builder.Append(";");
        else
        {
            builder.Append($" {_scope.GetSource()}");
        }

        return builder.ToString();
    }
}
