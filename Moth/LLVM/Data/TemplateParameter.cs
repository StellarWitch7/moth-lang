namespace Moth.LLVM.Data;

public class TemplateParameter
{
    public string Name { get; }
    public TemplateParameterBound[] Bounds { get; }
    public InternalType TypeOfConst { get => IsConst ? _typeOfConst : null; }
    public bool IsConst { get; }

    private InternalType _typeOfConst;

    public TemplateParameter(string name, TemplateParameterBound[] bounds, bool isConst, InternalType typeOfConst = null)
    {
        Name = name;
        Bounds = bounds;
        IsConst = isConst;
        _typeOfConst = typeOfConst;
    }
}
