namespace Moth.MIR;

public class MIRValue : MIROp
{
    public string Name { get; }
    public MIRType Type { get; }

    public MIRValue(string name, MIRType type)
    {
        Name = name;
        Type = type;
    }
}
