namespace Moth.LLVM.Data;

public class Variable : Pointer
{
    public virtual string Name { get; }
    public override VarType InternalType { get; }
    
    public Variable(string name, VarType type, LLVMValueRef llvmVariable) : base(null, llvmVariable)
    {
        Name = name;
        InternalType = type;
    }
    
    public override string GetInvalidTypeErrorMsg(Value value)
    {
        return $"Tried to assign value of type \"{value.InternalType}\" to variable of type \"{InternalType.BaseType}\".";
    }
}
