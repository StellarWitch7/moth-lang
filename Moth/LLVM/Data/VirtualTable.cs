namespace Moth.LLVM.Data;

public class VTableDef
{
    protected Dictionary<AspectMethod, LLVMValueRef> Table { get; } =
        new Dictionary<AspectMethod, LLVMValueRef>();
    protected int Count
    {
        get => Table.Count;
    }

    public LLVMValueRef[] GetIndex(AspectMethod funcDef)
    {
        return new LLVMValueRef[] { Table[funcDef] };
    }

    public void Add(AspectMethod funcDef)
    {
        var index = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)Count);
        Table.Add(funcDef, index);
    }
}

public class VTableInst
{
    public VTableDef Definition { get; }
    public LLVMValueRef LLVMValue { get; }

    public VTableInst(VTableDef definition)
    {
        Definition = definition;
    }
}
