namespace Moth.AST.Node;

public class InferredLocalDefNode : LocalDefNode
{
    public ExpressionNode Value { get; set; }

    public InferredLocalDefNode(string name, ExpressionNode val) : base(name, null)
    {
        Value = val;
    }
}
