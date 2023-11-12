namespace Moth.LLVM.Data;

public class Variable : CompilerData
{
    public string Name { get; set; }
    public LLVMValueRef LLVMVariable { get; set; }
    public ClassType Type { get; set; }

    public Variable(string name, LLVMValueRef llvmVariable, ClassType type)
    {
        Name = name;
        LLVMVariable = llvmVariable;
        Type = type;
    }
}
