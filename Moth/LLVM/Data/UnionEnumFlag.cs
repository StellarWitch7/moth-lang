namespace Moth.LLVM.Data;

public class UnionEnumFlag : EnumFlag
{
    public List<Type> UnionTypes { get; }

    public UnionEnumFlag(string name, ulong value, List<Type> unionTypes)
        : base(name, value)
    {
        UnionTypes = unionTypes;
    }
}
