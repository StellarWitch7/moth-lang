namespace Moth.AST.Node;

public class TraitTemplateNode : TraitNode, ITopDeclNode
{
    public TraitTemplateNode(
        string name,
        PrivacyType privacy,
        List<IMemberDeclNode>? contents,
        List<AttributeNode> attributes
    )
        : base(name, privacy, contents, attributes)
    {
        throw new NotImplementedException(); //TODO
    }
}
