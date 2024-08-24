using Moth.LLVM;

namespace Moth.AST.Node;

public class FuncDefNode : DefinitionNode, ITopDeclNode, IMemberDeclNode
{
    public List<ParameterNode> Params { get; set; }
    public ScopeNode? ExecutionBlock { get; set; }
    public ITypeRefNode ReturnTypeRef { get; set; }
    public bool IsVariadic { get; set; }
    public bool IsStatic { get; set; }

    public FuncDefNode(
        string name,
        PrivacyType privacy,
        ITypeRefNode returnTypeRef,
        List<ParameterNode> @params,
        ScopeNode? executionBlock,
        bool isVariadic,
        bool isStatic,
        List<AttributeNode> attributes
    )
        : base(name, privacy, attributes)
    {
        ReturnTypeRef = returnTypeRef;
        Params = @params;
        ExecutionBlock = executionBlock;
        IsVariadic = isVariadic;
        IsStatic = isStatic;
    }

    public override void GetSource(StringBuilder builder)
    {
        string @params = String.Join(
            ", ",
            Params
                .ToArray()
                .ExecuteOverAll(p =>
                {
                    return p.GetSource();
                })
        );

        if (Privacy != PrivacyType.Priv)
            builder.Append($"{Privacy} ".ToLower());

        if (IsStatic)
            builder.Append($"{Reserved.Static} ");

        if (IsVariadic)
            @params = $"{@params}, ...";

        builder.Append($"{Reserved.Function} {Name}({@params}) {ReturnTypeRef.GetSource()}");

        if (ExecutionBlock != default)
            builder.Append($" {ExecutionBlock.GetSource()}");
        else
            builder.Append(";");
    }
}
