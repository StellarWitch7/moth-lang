namespace Moth.LLVM.Data;

public class Variable : Pointer
{
    public virtual string Name { get; }
    public override RefType Type { get; }
    
    public Variable(string name, RefType type, LLVMValueRef llvmVariable) : base(null, llvmVariable)
    {
        Name = name;
        Type = type;
    }
    
    public override string GetInvalidTypeErrorMsg(Value value)
    {
        return $"Tried to assign value of type \"{value.Type}\" to variable of type \"{Type.BaseType}\".";
    }

    public override Value SafeLoad(LLVMCompiler compiler)
    {
        try
        {
            return Load(compiler);
        }
        catch
        {
            return this; //TODO: is this correct?
            // throw new Exception($"Failed to load variable \"{Name}\"");
        }
    }
}
