namespace Moth.AST.Node;

public class FuncCallNode : ExpressionNode
{
    public string Name { get; set; }
    public IReadOnlyList<ExpressionNode> Arguments { get; set; }
    public ExpressionNode ToCallOn { get; set; }

    public FuncCallNode(string name, IReadOnlyList<ExpressionNode> args, ExpressionNode callOn)
    {
        Name = name;
        Arguments = args;
        ToCallOn = callOn;
    }
}
