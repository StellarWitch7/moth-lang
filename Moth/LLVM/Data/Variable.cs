namespace Moth.LLVM.Data;

public class Variable : Value
{
    public string Name { get; set; }

    public Variable(string name, Type type, LLVMValueRef llvmVariable) : base(type, llvmVariable)
    {
        Name = name;
    }
}
