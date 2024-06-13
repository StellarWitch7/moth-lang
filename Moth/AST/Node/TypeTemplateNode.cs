namespace Moth.AST.Node;

public class TypeTemplateNode : TypeNode
{
    public List<TemplateParameterNode> Params { get; set; }

    public TypeTemplateNode(
        string name,
        PrivacyType privacy,
        List<TemplateParameterNode> @params,
        ScopeNode scope,
        bool isUnion,
        List<AttributeNode> attributes
    )
        : base(name, privacy, scope, isUnion, attributes) => Params = @params;

    public override string GetSource()
    {
        string key = $"{Reserved.Type} {Name}";
        string s = base.GetSource();

        return s.Insert(
            s.IndexOf(key) + key.Length,
            $"<{String.Join(", ", Params.ToArray().ExecuteOverAll(p => p.Name))}>"
        );
    }
}
