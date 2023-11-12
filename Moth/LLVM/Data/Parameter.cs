namespace Moth.LLVM.Data;

public class Parameter : CompilerData
{
    public uint ParamIndex { get; set; }
    public string Name { get; set; }
    public ClassType Type { get; set; }

    public Parameter(uint paramIndex, string name, ClassType type)
    {
        ParamIndex = paramIndex;
        Name = name;
        Type = type;
    }
}
