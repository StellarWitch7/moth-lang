namespace Moth.AST.Node;

public class AttributeNode : IASTNode
{
    public string Name { get; set; }
    public List<IExpressionNode> Arguments { get; set; }

    public AttributeNode(string name, List<IExpressionNode> arguments)
    {
        Name = name;
        Arguments = arguments;
    }

    public string GetSource()
    {
        string s = $"@{Name}";

        if (Arguments.Count > 0)
            s = $"{s}({String.Join(", ", Arguments.ToArray().ExecuteOverAll(a => a.GetSource()))})";

        return s;
    }
}
