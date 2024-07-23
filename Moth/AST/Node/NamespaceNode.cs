namespace Moth.AST.Node;

public class NamespaceNode : IExpressionNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
    public string Name { get; set; }
    public NamespaceNode? Child { get; set; }

    public NamespaceNode(string name)
    {
        Name = name;
    }

    public string GetSource()
    {
        if (Child == default)
            return $"{Name}";

        return $"{Name}::{Child.GetSource()}";
    }
}
