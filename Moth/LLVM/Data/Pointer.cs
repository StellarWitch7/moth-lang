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

public class AspectPointer : Pointer
{
    public override AspectPtrType Type { get; }
    public TraitDecl TraitDecl { get => Type.BaseType; }

    public AspectPointer(AspectPtrType type, LLVMValueRef llvmValue) : base(type, llvmValue)
    {
        Type = type;
    }

    public Value CallMethod(LLVMCompiler compiler, AspectMethod method, Value[] args)
    {
        var index = Type.BaseType.VTable.GetIndex(method);
        var vtable = compiler.Builder.BuildExtractElement(LLVMValue, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1));
        var func = compiler.Builder.BuildInBoundsGEP2(method.Type.LLVMType, vtable, index);
        return method.Type.Call(compiler, func, args);
    }
    
    public override Pointer Store(LLVMCompiler compiler, Value value)
    {
        throw new NotImplementedException(); //TODO: this is an illegal function to call
    }

    public override Value DeRef(LLVMCompiler compiler)
    {
        throw new NotImplementedException(); //TODO: this is an illegal function to call
    }
}