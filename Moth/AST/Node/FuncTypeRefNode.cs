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
        var builder = new StringBuilder($"#(");
        builder.Append(
            String.Join(
                ", ",
                ParameterTypes
                    .ToArray()
                    .ExecuteOverAll(t =>
                    {
                        return t.GetSource();
                    })
            )
        );
        builder.Append(")");

        for (uint i = PointerDepth; i > 0; i--)
            builder.Append("*");

        return $"{builder} {ReturnType.GetSource()}";
    }
}
