namespace Moth.MIR;

public class MIRBlock
{
    public string Name { get; }
    public MIRFunction Parent { get; }

    private bool _hasReturned = false;
    private List<MIROp> _statements { get; } = new List<MIROp>();

    public MIRBlock(string name, MIRFunction parent)
    {
        Name = name;
        Parent = parent;
    }

    public int Length
    {
        get => _statements.Count;
    }
    public bool HasReturned
    {
        get => _hasReturned;
        set
        {
            if (value == false && _hasReturned)
                throw new Exception($"Block \"{this}\" has returned, this cannot be undone.");

            _hasReturned = value;
        }
    }

    public void AddInstruction(MIROp op, int pos)
    {
        if (HasReturned)
            throw new Exception(
                $"Block \"{this}\" in function \"{Parent}\" has already returned, cannot add more instructions."
            );

        _statements.Insert(pos, op);
    }
}

public enum BlockPos
{
    Start,
    End,
}
