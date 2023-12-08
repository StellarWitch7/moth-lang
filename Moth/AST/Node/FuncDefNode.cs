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
}
