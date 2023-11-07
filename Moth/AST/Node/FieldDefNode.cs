namespace Moth.AST.Node;

public class FieldDefNode : MemberDefNode
{
    public string Name { get; set; }
    public PrivacyType Privacy { get; set; }
    public TypeRefNode TypeRef { get; set; }

    public FieldDefNode(string name, PrivacyType privacy, TypeRefNode typeRef, List<AttributeNode> attributes)
        : base(attributes)
    {
        Name = name;
        Privacy = privacy;
        TypeRef = typeRef;
    }
}