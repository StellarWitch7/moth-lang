namespace Moth.AST.Node;

public class TemplateTypeRefNode : TypeRefNode
{
    public List<ExpressionNode> Arguments { get; set; }

    public TemplateTypeRefNode(string name, List<ExpressionNode> args, uint pointerDepth = 0) : base(name, pointerDepth) => Arguments = args;
}
