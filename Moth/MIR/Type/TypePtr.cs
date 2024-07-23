namespace Moth.MIR.Type;

public class TypePtr : MIRType
{
    public MIRType BaseType { get; }

    public TypePtr(MIRType baseType)
    {
        BaseType = baseType;
    }

    public override string ToString()
    {
        return $"{BaseType}*";
    }
}
