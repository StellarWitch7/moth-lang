namespace Moth.AST.Node;

public class FuncTypeRefNode : TypeRefNode
{
    public TypeRefNode ReturnType { get; set; }
    public List<TypeRefNode> ParameterTypes { get; set; }

    public FuncTypeRefNode(
        TypeRefNode retType,
        List<TypeRefNode> @params,
        uint pointerDepth,
        bool isRef
    )
        : base(null, pointerDepth, isRef)
    {
        ReturnType = retType;
        ParameterTypes = @params;
    }

    public override string GetSource()
    {
        return $"#{GetSourceForParamTypes()} {ReturnType.GetSource()}";
    }

    private string GetSourceForParamTypes()
    {
        var builder = new StringBuilder("(");

        foreach (TypeRefNode paramType in ParameterTypes)
        {
            builder.Append($"{paramType.GetSource()}, ");
        }

        if (builder.Length > 1)
            builder.Remove(builder.Length - 2, 2);

        builder.Append(")");
        return builder.ToString();
    }
}
