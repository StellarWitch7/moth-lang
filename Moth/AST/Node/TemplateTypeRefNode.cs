namespace Moth.AST.Node;

public class TemplateTypeRefNode : TypeRefNode
{
    public List<ExpressionNode> Arguments { get; set; }

    public TemplateTypeRefNode(string name, List<ExpressionNode> args, uint pointerDepth, bool isRef) : base(name, pointerDepth, isRef) => Arguments = args;
}
