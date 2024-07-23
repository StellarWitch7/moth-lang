using Moth.MIR.Op;

namespace Moth.MIR;

public class MIRBuilder
{
    public MIRModule Module { get; }
    public MIRBlock Block { get; private set; }
    public int PositionInBlock { get; private set; }

    public MIRBuilder(MIRModule module)
    {
        Module = module;
    }

    public MIRBuilder(MIRModule module, MIRBlock block, BlockPos pos = BlockPos.Start)
        : this(module)
    {
        PositionAt(block, pos);
    }

    public void PositionAt(MIRBlock block, BlockPos pos)
    {
        Block = block;
        PositionInBlock = pos switch
        {
            BlockPos.Start => 0,
            BlockPos.End => block.Length,
            _ => throw new NotImplementedException($"{pos} is not implemented.")
        };
    }

    public void AddInstruction(MIROp op)
    {
        Block.AddInstruction(op, PositionInBlock);
        PositionInBlock++;
    }

    public string GenName()
    {
        throw new NotImplementedException(); //TODO
    }

    public MIROp BuildRet(MIRValue value = null)
    {
        MIROp op;

        if (value is null)
            op = new OpRetVoid();
        else
        {
            if (!value.Type.Equals(Block.Parent.ReturnType))
                throw new Exception(
                    $"Value \"{value}\" cannot be returned by function \"{Block.Parent}\" as it is not of type \"{Block.Parent.ReturnType}\"."
                );

            op = new OpRetVal(value);
        }

        AddInstruction(op);
        Block.HasReturned = true;
        return op;
    }

    public MIRValue BuildAlloca(MIRType type)
    {
        var op = new OpAlloca(GenName(), type);
        AddInstruction(op);
        return op;
    }
}
