namespace Moth.LLVM.Data;

public class Scope : CompilerData
{
    public LLVMBasicBlockRef LLVMBlock { get; set; }
    public Dictionary<string, Variable> Variables { get; } = new Dictionary<string, Variable>();

    public Scope(LLVMBasicBlockRef llvmBlock)
    {
        LLVMBlock = llvmBlock;
    }
}
