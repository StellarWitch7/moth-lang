namespace Moth.LLVM.Data;

public class Variable : Pointer
{
    public virtual string Name { get; }
    public override VarType Type { get; }

    public Variable(LLVMCompiler compiler, string name, VarType type, LLVMValueRef llvmVariable)
        : base(compiler, null, llvmVariable)
    {
        Name = name;
        Type = type;
    }

    public override string GetInvalidTypeErrorMsg(Value value)
    {
        return $"Tried to assign value of type \"{value.Type}\" to variable of type \"{Type.BaseType}\".";
    }
}
