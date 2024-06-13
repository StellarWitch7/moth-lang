using Moth.AST.Node;

namespace Moth.AST;

public interface IDefinitionNode : IStatementNode
{
    List<AttributeNode> Attributes { get; set; }
    PrivacyType Privacy { get; set; }
}

public enum PrivacyType
{
    Priv,
    Pub,
}
