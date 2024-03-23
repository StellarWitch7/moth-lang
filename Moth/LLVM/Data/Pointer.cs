using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Pointer : Value
{
    public override PtrType Type { get; }
    
    public Pointer(PtrType type, LLVMValueRef llvmValue) : base(null, llvmValue)
    {
        Type = type;
    }

    public virtual Pointer Store(LLVMCompiler compiler, Value value)
    {
        if (!Type.BaseType.Equals(value.Type))
        {
            throw new Exception(GetInvalidTypeErrorMsg(value));
        }
        
        compiler.Builder.BuildStore(value.LLVMValue, LLVMValue);
        return this;
    }
    
    public override Value DeRef(LLVMCompiler compiler) //TODO: might need adjusting for the new type engine
    {
        if (!Type.BaseType.Equals(Primitives.Void))
        {
            return Value.Create(Type.BaseType, compiler.Builder.BuildLoad2(Type.BaseType.LLVMType, LLVMValue));
        }
        else
        {
            throw new Exception("Cannot load pointer to void.");
        }
    }

    public virtual string GetInvalidTypeErrorMsg(Value value)
    {
        return $"Tried to assign value of type \"{value.Type}\" to pointer of type \"{Type.BaseType}\".";
    }
}
