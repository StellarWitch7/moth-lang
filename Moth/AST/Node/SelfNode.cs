namespace Moth.AST.Node;

public class SelfNode : IExpressionNode
{
    public string GetSource()
    {
        return Reserved.Self;
    }
}
