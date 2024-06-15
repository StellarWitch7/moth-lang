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

    public string GetSource()
    {
        var builder = new StringBuilder(
            Attributes.Count > 0
                ? $"{String.Join("\n", Attributes.ToArray().ExecuteOverAll(a => a.GetSource()))}\n"
                : String.Empty
        );
        GetSource(builder);
        builder.Append("\n");
        return builder.ToString();
    }

    public abstract void GetSource(StringBuilder builder);
}

public enum PrivacyType
{
    Priv,
    Pub,
}
