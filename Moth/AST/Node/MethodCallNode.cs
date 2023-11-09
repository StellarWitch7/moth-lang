namespace Moth.AST.Node;

public class FuncCallNode : RefNode
{
    public List<ExpressionNode> Arguments { get; set; }

    public FuncCallNode(string name, List<ExpressionNode> args) : base(name) => Arguments = args;
}