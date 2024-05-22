namespace Moth.AST.Node;

public class FuncDefNode : DefinitionNode
{
    public string Name { get; set; }
    public List<ParameterNode> Params { get; set; }
    public ScopeNode? ExecutionBlock { get; set; }
    public PrivacyType Privacy { get; set; }
    public TypeRefNode ReturnTypeRef { get; set; }
    public bool IsVariadic { get; set; }
    public bool IsStatic { get; set; }
    public bool IsForeign { get; set; }

    public FuncDefNode(string name, PrivacyType privacyType, TypeRefNode returnTypeRef,
        List<ParameterNode> @params, ScopeNode? executionBlock, bool isVariadic, bool isStatic, bool isForeign,
        List<AttributeNode> attributes) : base(attributes)
    {
        Name = name;
        Privacy = privacyType;
        ReturnTypeRef = returnTypeRef;
        Params = @params;
        ExecutionBlock = executionBlock;
        IsVariadic = isVariadic;
        IsStatic = isStatic;
        IsForeign = isForeign;
    }

    public override string GetSource()
    {
        var builder = new StringBuilder();

        if (Privacy != PrivacyType.Priv)
            builder.Append($"{Privacy} ".ToLower());

        if (IsForeign)
            builder.Append("foreign ");

        if (IsStatic)
            builder.Append("static ");

        builder.Append($"fn {Name}{GetSourceForParams()} {ReturnTypeRef.GetSource()}");

        if (ExecutionBlock != default)
            builder.Append($" {ExecutionBlock.GetSource()}");
        else
            builder.Append(";");

        return builder.ToString();
    }

    private string GetSourceForParams()
    {
        var builder = new StringBuilder("(");

        foreach (ParameterNode param in Params)
        {
            builder.Append($"{param.GetSource()}, ");
        }

        if (builder.Length > 1)
            builder.Remove(builder.Length - 2, 2);
        
        builder.Append(")");
        return builder.ToString();
    }
}
