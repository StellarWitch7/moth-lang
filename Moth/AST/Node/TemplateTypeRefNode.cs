namespace Moth.AST.Node;

public class TemplateTypeRefNode : TypeRefNode
{
    public List<ExpressionNode> Arguments { get; set; }

    public TemplateTypeRefNode(string name, List<ExpressionNode> args, uint pointerDepth, bool isRef) : base(name, pointerDepth, isRef) => Arguments = args;

    public override string GetSource()
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
