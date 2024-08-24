namespace Moth.AST.Node;

public class FuncTypeRefNode : ITypeRefNode
{
    public ITypeRefNode ReturnType { get; set; }
    public List<ITypeRefNode> ParameterTypes { get; set; }

    public FuncTypeRefNode(ITypeRefNode retType, List<ITypeRefNode> @params)
    {
        ReturnType = retType;
        ParameterTypes = @params;
    }

    public string GetSource()
    {
        return GetSource(false);
    }

    public string GetSource(bool asChild)
    {
        string @base =
            $"({String.Join(
            ", ",
            ParameterTypes
                .ToArray()
                .ExecuteOverAll(t =>
                {
                    return t.GetSource();
                })
        )}) {ReturnType.GetSource()}";

        return asChild ? $"#({@base})" : $"#{@base}";
    }
}
