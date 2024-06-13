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
        throw new NotImplementedException();
    }
}
