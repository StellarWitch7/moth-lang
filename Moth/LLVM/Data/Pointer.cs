namespace Moth.LLVM.Data;

public class Pointer : Value
{
    public override PtrType Type { get; }
    
    public Pointer(PtrType type, LLVMValueRef llvmValue) : base(null, llvmValue)
    {
        Type = type;
    }

    public Value Store(LLVMCompiler compiler, Value value)
    {
        if (!Type.BaseType.Equals(value.Type))
        {
            throw new Exception($"Tried to assign value of type \"{value.Type}\" to variable of type \"{Type.BaseType}\".");
        }
        
        compiler.Builder.BuildStore(value.LLVMValue, LLVMValue);
        return this;
    }
}

public class Variable : Pointer
{
    public string Name { get; set; }
    public override RefType Type { get; }

    public Variable(string name, RefType type, LLVMValueRef llvmVariable) : base(null, llvmVariable)
    {
        Name = name;
        Type = type;
    }

}