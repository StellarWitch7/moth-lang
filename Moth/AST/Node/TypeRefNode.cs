namespace Moth.AST.Node;

public class TypeRefNode : ExpressionNode
{
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

    public override string GetSource()
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
