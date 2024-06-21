namespace Moth.AST.Node;

public class IndexAccessNode : IExpressionNode
{
    public IExpressionNode ToBeIndexed { get; set; }
    public List<IExpressionNode> Arguments { get; set; }

    public IndexAccessNode(IExpressionNode toBeIndexed, List<IExpressionNode> arguments)
    {
        ToBeIndexed = toBeIndexed;
        Arguments = arguments;
    }

    public string GetSource()
    {
        return $"{ToBeIndexed.GetSource()}[{String.Join(", ", Arguments.ToArray().ExecuteOverAll(e => {
            return e.GetSource();
        }))}]";
    }
}
