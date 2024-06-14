using Moth.LLVM;

namespace Moth.AST.Node;

public class FuncDefNode : DefinitionNode
{
    public List<ParameterNode> Params { get; set; }
    public ScopeNode? ExecutionBlock { get; set; }
    public TypeRefNode ReturnTypeRef { get; set; }
    public bool IsVariadic { get; set; }
    public bool IsStatic { get; set; }
    public bool IsForeign { get; set; }

    public FuncDefNode(
        string name,
        PrivacyType privacy,
        TypeRefNode returnTypeRef,
        List<ParameterNode> @params,
        ScopeNode? executionBlock,
        bool isVariadic,
        bool isStatic,
        bool isForeign,
        List<AttributeNode> attributes
    )
        : base(name, privacy, attributes)
    {
        ReturnTypeRef = returnTypeRef;
        Params = @params;
        ExecutionBlock = executionBlock;
        IsVariadic = isVariadic;
        IsStatic = isStatic;
        IsForeign = isForeign;
    }

    public override string GetSource()
    {
        var builder = new StringBuilder("\n");
        string @params = String.Join(
            ", ",
            Params
                .ToArray()
                .ExecuteOverAll(p =>
                {
                    return p.GetSource();
                })
        );

        builder.Append(GetAttributeSource());

        if (Privacy != PrivacyType.Priv)
            builder.Append($"{Privacy} ".ToLower());

        if (IsForeign)
            builder.Append($"{Reserved.Foreign} ");

        if (IsStatic)
            builder.Append($"{Reserved.Static} ");

        if (IsVariadic)
            @params = $"{@params}, ...";

        builder.Append($"{Reserved.Function} {Name}({@params}) {ReturnTypeRef.GetSource()}");

        if (ExecutionBlock != default)
            builder.Append($" {ExecutionBlock.GetSource()}");
        else
            builder.Append(";");

        return builder.ToString();
    }
}
