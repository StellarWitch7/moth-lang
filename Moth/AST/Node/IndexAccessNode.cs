namespace Moth.AST.Node;

public class IndexAccessNode : IExpressionNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
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
