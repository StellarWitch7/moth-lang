namespace Moth.LLVM.Data;

public class Scope : CompilerData, IContainer
{
    public IContainer? Parent { get; }
    public LLVMBasicBlockRef LLVMBlock { get; set; }
    public Dictionary<string, Variable> Variables { get; } = new Dictionary<string, Variable>();

    public Scope(IContainer? parent, LLVMBasicBlockRef llvmBlock)
    {
        Parent = parent;
        LLVMBlock = llvmBlock;
    }

    public CompilerData GetData(string name) => throw new NotImplementedException();

    public bool TryGetData(string name, out CompilerData data) => throw new NotImplementedException();
}
