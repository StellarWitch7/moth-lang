namespace Moth.LLVM.Data;

public class Parameter : CompilerData
{
    public uint ParamIndex { get; set; }
    public string Name { get; set; }
    public Type Type { get; set; }

    public Parameter(uint paramIndex, string name, Type type)
    {
        ParamIndex = paramIndex;
        Name = name;
        Type = type;
    }
}
