namespace Moth.LLVM.Data;

public class EnumFlag
{
    public string Name { get; }
    public ulong Value { get; }

    public EnumFlag(string name, ulong value)
    {
        Name = name;
        Value = value;
    }
}
