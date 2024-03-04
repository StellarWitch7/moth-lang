namespace Moth.AST.Node;

public class FuncCallNode : RefNode
{
    public IReadOnlyList<ExpressionNode> Arguments { get; set; }

    public FuncCallNode(string name, IReadOnlyList<ExpressionNode> args) : base(name) => Arguments = args;
}