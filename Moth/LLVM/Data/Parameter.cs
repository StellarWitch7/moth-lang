namespace Moth.LLVM.Data;

public class Parameter
{
    public uint ParamIndex { get; set; }
    public string Name { get; set; }

    public Parameter(uint paramIndex, string name)
    {
        ParamIndex = paramIndex;
        Name = name;
    }
}
