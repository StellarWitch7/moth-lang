namespace Moth.MIR.Type;

public class TypeFunction : MIRType
{
    public MIRType ReturnType { get; }
    public MIRType[] ParamTypes { get; }

    public TypeFunction(MIRType retType, MIRType[] paramTypes)
    {
        ReturnType = retType;
        ParamTypes = paramTypes;
    }

    public override string ToString()
    {
        return $"{ReturnType}({String.Join(", ", ParamTypes.ExecuteOverAll(v => $"{v}"))})";
    }
}
