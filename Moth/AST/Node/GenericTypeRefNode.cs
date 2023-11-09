namespace Moth.AST.Node;

public class GenericTypeRefNode : TypeRefNode
{
    private List<ExpressionNode> Arguments { get; set; }

    public GenericTypeRefNode(string name, List<ExpressionNode> args, uint pointerDepth = 0) : base(name, pointerDepth) => Arguments = args;
}
