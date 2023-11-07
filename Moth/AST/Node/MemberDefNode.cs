namespace Moth.AST.Node;

public class MemberDefNode : DefinitionNode
{
    public List<AttributeNode> Attributes { get; set; }

    public MemberDefNode(List<AttributeNode> attributes)
    {
        Attributes = attributes;
    }
}
