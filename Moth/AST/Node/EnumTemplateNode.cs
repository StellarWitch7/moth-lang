namespace Moth.AST.Node;

public class EnumTemplateNode : EnumNode, ITopDeclNode
{
    public EnumTemplateNode(
        string name,
        PrivacyType privacy,
        List<EnumFlagNode> enumFlags,
        ScopeNode? scope,
        List<AttributeNode> attributes
    )
        : base(name, privacy, enumFlags, scope, attributes)
    {
        throw new NotImplementedException(); //TODO
    }
}
