namespace Moth.AST.Node;

public class TypeRefNode : IExpressionNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
    public string Name { get; set; }
    public uint PointerDepth { get; set; }
    public bool IsRef { get; set; }
    public NamespaceNode? Namespace { get; set; }

    public TypeRefNode(string name, uint pointerDepth, bool isRef)
    {
        Name = name;
        PointerDepth = pointerDepth;
        IsRef = isRef;
    }

    public virtual string GetSource()
    {
        var builder = new StringBuilder();

        if (Namespace != default)
            builder.Append(Namespace.GetSource());

        builder.Append($"#{Name}");

        for (uint i = PointerDepth; i > 0; i--)
            builder.Append("*");

        if (IsRef)
            builder.Append("&");

        return builder.ToString();
    }
}
