namespace Moth.AST.Node;

public class TemplateTypeRefNode : NamedTypeRefNode
{
    public List<IExpressionNode> Arguments { get; set; }

    public TemplateTypeRefNode(string name, List<IExpressionNode> args)
        : base(name)
    {
        Arguments = args;
    }

    public string GetSource()
    {
        return $"{base.GetSource()}{GetSourceForTemplateArgs()}";
    }

    private string GetSourceForTemplateArgs()
    {
        var builder = new StringBuilder("<");

        foreach (var arg in Arguments)
        {
            builder.Append($"{arg.GetSource()}, ");
        }

        if (builder.Length > 1)
            builder.Remove(builder.Length - 2, 2);

        builder.Append(">");
        return builder.ToString();
    }
}
