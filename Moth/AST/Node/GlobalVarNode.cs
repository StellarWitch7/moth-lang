namespace Moth.AST.Node;

public class GlobalVarNode : StatementNode
{
    public string Name { get; set; }
    public TypeRefNode TypeRef { get; set; }
    public PrivacyType Privacy { get; set; }
    public bool IsConstant { get; set; }

    public GlobalVarNode(string name, TypeRefNode typeRef, PrivacyType privacy, bool isConstant)
    {
        Name = name;
        TypeRef = typeRef;
        Privacy = privacy;
        IsConstant = isConstant;
    }
}
