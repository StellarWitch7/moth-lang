namespace Moth.MIR;

public class MIRModule
{
    public string Name { get; }

    public MIRModule(string name)
    {
        Name = name;
    }

    public MIRBuilder BuilderAt(MIRBlock block, BlockPos pos)
    {
        var builder = new MIRBuilder(this);
        builder.PositionAt(block, pos);
        return builder;
    }
}
