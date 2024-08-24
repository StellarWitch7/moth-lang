namespace Moth.AST.Node;

public class TraitNode : MemberContainingDefinitionNode, ITopDeclNode
{
    public TraitNode(
        string name,
        PrivacyType privacy,
        List<IMemberDeclNode>? contents,
        List<AttributeNode>? attributes
    )
        : base(name, privacy, contents, attributes) { }

    public override void GetSource(StringBuilder builder)
    {
        throw new NotImplementedException();
    }
}
