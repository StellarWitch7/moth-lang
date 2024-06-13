namespace Moth.AST.Node;

public class InferredLocalDefNode : LocalDefNode
{
    public IExpressionNode Value { get; set; }

    public InferredLocalDefNode(string name, IExpressionNode val)
        : base(name, null) => Value = val;
}
