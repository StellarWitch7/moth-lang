namespace Moth.AST.Node;

public class FuncTypeRefNode : TypeRefNode
{
    public TypeRefNode ReturnType { get; set; }
    public List<TypeRefNode> ParamterTypes { get; set; }

    public FuncTypeRefNode(TypeRefNode retType, List<TypeRefNode> @params, uint pointerDepth = 0)
        : base(null, pointerDepth)
    {
        ReturnType = retType;
        ParamterTypes = @params;
    }
}
