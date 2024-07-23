namespace Moth.AST.Node;

public class RefNode : IExpressionNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
    public string Name { get; set; }
    public IExpressionNode? Parent { set; get; }

    public RefNode(string name, IExpressionNode? parent)
    {
        Name = name;
        Parent = parent;
    }

    public virtual string GetSource()
    {
        if (Parent == null)
            return Name;

        return $"{Parent.GetSource()}.{Name}";
    }
}
