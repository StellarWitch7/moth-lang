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

    public override Value SafeLoad(LLVMCompiler compiler)
    {
        try {
            if (Type is RefType) {
                return Load(compiler);
            }
        }
        catch { }

        return this;
    }

    public Value Load(LLVMCompiler compiler)
    {
        if (Type.BaseType is FuncType fnT)
        {
            return new Function(fnT,
                compiler.Builder.BuildLoad2(Type.BaseType.LLVMType, LLVMValue),
                new Parameter[0]);
        }
        else if (!Type.BaseType.Equals(Primitives.Void))
        {
            return Value.Create(Type.BaseType, compiler.Builder.BuildLoad2(Type.BaseType.LLVMType, LLVMValue));
        }
        else
        {
            throw new Exception("Failed to load pointer!");
        }
    }

    public virtual string GetInvalidTypeErrorMsg(Value value)
    {
        return $"Tried to assign value of type \"{value.Type}\" to pointer of type \"{Type.BaseType}\".";
    }
}
