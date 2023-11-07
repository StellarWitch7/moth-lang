namespace Moth.AST.Node;

public class FuncDefNode : MemberDefNode
{
    public string Name { get; set; }
    public List<ParameterNode> Params { get; set; }
    public ScopeNode ExecutionBlock { get; set; }
    public PrivacyType Privacy { get; set; }
    public TypeRefNode ReturnTypeRef { get; set; }
    public bool IsVariadic { get; set; }

    public FuncDefNode(string name, PrivacyType privacyType, TypeRefNode returnTypeRef,
        List<ParameterNode> @params, ScopeNode executionBlock, bool isVariadic, List<AttributeNode> attributes)
        : base(attributes)
    {
        Name = name;
        Privacy = privacyType;
        ReturnTypeRef = returnTypeRef;
        Params = @params;
        ExecutionBlock = executionBlock;
        IsVariadic = isVariadic;
    }
}
