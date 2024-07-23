namespace Moth.AST.Node;

public class ImportNode : IStatementNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
    public NamespaceNode Namespace { get; set; }

    public ImportNode(NamespaceNode nmspace)
    {
        Namespace = nmspace;
    }

    public string GetSource() => $"{Reserved.With} {Namespace.GetSource()};";
}
