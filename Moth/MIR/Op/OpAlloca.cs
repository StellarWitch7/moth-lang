using Moth.MIR.Type;

namespace Moth.MIR.Op;

public class OpAlloca : MIRValue
{
    public MIRType BaseType { get; }

    public OpAlloca(string name, MIRType type)
        : base(name, new TypePtr(type))
    {
        if (type is TypeVoid)
            throw new Exception($"Cannot allocate space for void.");

        BaseType = type;
    }

    public override string ToString()
    {
        return $"%{Name} = alloca {BaseType}";
    }
}
