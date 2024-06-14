using Moth.AST.Node;

namespace Moth.AST.Node;

public abstract class DefinitionNode : IStatementNode
{
    public string Name { get; set; }
    public PrivacyType Privacy { get; set; }
    public List<AttributeNode> Attributes { get; set; }

    public DefinitionNode(string name, PrivacyType privacy, List<AttributeNode>? attributes)
    {
        Name = name;
        Privacy = privacy;
        Attributes = attributes ?? new List<AttributeNode>();
    }

    public abstract string GetSource();

    public string GetAttributeSource()
    {
        if (Attributes.Count > 0)
            return $"{String.Join("\n", Attributes.ToArray().ExecuteOverAll(a => a.GetSource()))}\n";

        return String.Empty;
    }
}

public enum PrivacyType
{
    Priv,
    Pub,
}
