using Moth.MIR.Type;

namespace Moth.MIR;

public class MIRFunction
{
    public string Name { get; }
    public TypeFunction Type { get; }
    public MIRModule Module { get; }

    public MIRFunction(string name, MIRModule module, TypeFunction type)
    {
        Name = name;
        Type = type;
        Module = module;
    }

    public MIRType ReturnType
    {
        get => Type.ReturnType;
    }

    public MIRType[] ParamTypes
    {
        get => Type.ParamTypes;
    }
}
