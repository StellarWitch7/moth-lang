namespace Moth.MIR.Op;

public class OpRetVal : MIROp
{
    public MIRValue Value { get; }

    public OpRetVal(MIRValue value)
    {
        Value = value;
    }
}
