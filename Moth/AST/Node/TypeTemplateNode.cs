namespace Moth.AST.Node;

public class TypeTemplateNode : TypeNode, ITopDeclNode
{
    public List<TemplateParameterNode> Params { get; set; }

    public TypeTemplateNode(
        string name,
        PrivacyType privacy,
        List<TemplateParameterNode> @params,
        List<IMemberDeclNode>? contents,
        bool isUnion,
        List<AttributeNode> attributes
    )
        : base(name, privacy, contents, isUnion, attributes) => Params = @params;

    public override void GetSource(StringBuilder builder)
    {
        string key = $"{Reserved.Type} {Name}";
        base.GetSource(builder);
        builder.Insert(
            builder.ToString().IndexOf(key) + key.Length,
            $"<{String.Join(", ", Params.ToArray().ExecuteOverAll(p => p.Name))}>"
        );
    }
}
