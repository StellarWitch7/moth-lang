namespace Moth.AST.Node;

public class ImportNode : ITopDeclNode
{
    public NamespaceNode Namespace { get; set; }

    public ImportNode(NamespaceNode nmspace)
    {
        Namespace = nmspace;
    }

    public string GetSource() => $"{Reserved.With} {Namespace.GetSource()};";
}
