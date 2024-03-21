namespace Moth.AST.Node;

public class FuncTypeRefNode : TypeRefNode
{
    public TypeRefNode ReturnType { get; set; }
    public List<TypeRefNode> ParamterTypes { get; set; }

    public FuncTypeRefNode(TypeRefNode retType, List<TypeRefNode> @params, uint pointerDepth, bool isRef)
        : base(null, pointerDepth, isRef)
    {
        ReturnType = retType;
        ParamterTypes = @params;
    }
}
