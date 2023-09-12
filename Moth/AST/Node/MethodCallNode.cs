namespace Moth.AST.Node;

public class MethodCallNode : RefNode
{
    public List<ExpressionNode> Arguments { get; }

    public MethodCallNode(string name, List<ExpressionNode> arguments) : base(name)
    {
        Arguments = arguments;
    }
}