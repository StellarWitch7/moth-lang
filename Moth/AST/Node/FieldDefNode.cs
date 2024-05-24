namespace Moth.AST.Node;

public class FieldDefNode : DefinitionNode
{
    public string Name { get; set; }
    public PrivacyType Privacy { get; set; }
    public TypeRefNode TypeRef { get; set; }

    public FieldDefNode(
        string name,
        PrivacyType privacy,
        TypeRefNode typeRef,
        List<AttributeNode> attributes
    )
        : base(attributes)
    {
        Name = name;
        Privacy = privacy;
        TypeRef = typeRef;
    }

    public override string GetSource()
    {
        var builder = new StringBuilder();

        if (Privacy != PrivacyType.Priv)
            builder.Append($"{Privacy} ".ToLower());

        builder.Append($"{Name} {TypeRef.GetSource()};");
        return builder.ToString();
    }
}
