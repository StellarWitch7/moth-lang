namespace Moth.LLVM.Data;

public class Scope : CompilerData
{
    public LLVMBasicBlockRef LLVMBlock { get; set; }
    public Dictionary<string, Variable> Variables { get; set; } = new Dictionary<string, Variable>();

    public Scope(LLVMBasicBlockRef llvmBlock) => LLVMBlock = llvmBlock;

    public Variable GetVariable(string name)
    {
        return Variables.TryGetValue(name, out Variable @var)
            ? @var
            : throw new Exception($"Variable \"{name}\" does not exist in the current scope.");
    }
}
