namespace Moth.LLVM.Data;

public class Variable : Value
{
    public string Name { get; set; }
    public override RefType Type { get; }

    public Variable(string name, RefType type, LLVMValueRef llvmVariable) : base(null, llvmVariable)
    {
        Name = name;
        Type = type;
    }
}
