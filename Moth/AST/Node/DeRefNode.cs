namespace Moth.AST.Node;

public class DeRefNode : IExpressionNode
{
    public IExpressionNode Value { get; set; }

    public DeRefNode(IExpressionNode value) => Value = value;

    public string GetSource()
    {
        throw new NotImplementedException();
    }
}
