namespace Moth.AST.Node;

public class DefinitionNode : StatementNode
{
    public List<AttributeNode> Attributes { get; set; }

    public DefinitionNode(List<AttributeNode> attributes) => Attributes = attributes;
}

public enum PrivacyType
{
    Priv,
    Pub,
}
