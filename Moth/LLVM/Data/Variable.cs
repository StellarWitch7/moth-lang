namespace Moth.LLVM.Data;

public class Variable : Pointer
{
    public virtual string Name { get; }
    public override VarType Type { get; }
    
    public Variable(string name, VarType type, LLVMValueRef llvmVariable) : base(null, llvmVariable)
    {
        Name = name;
        Type = type;
    }
    
    public override string GetInvalidTypeErrorMsg(Value value)
    {
        return $"Tried to assign value of type \"{value.Type}\" to variable of type \"{Type.BaseType}\".";
    }
}
