using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Pointer : Value
{
    public override PtrType InternalType { get; }
    
    public Pointer(PtrType type, LLVMValueRef llvmValue) : base(null, llvmValue)
    {
        InternalType = type;
    }

    public virtual Pointer Store(LLVMCompiler compiler, Value value)
    {
        if (!InternalType.BaseType.Equals(value.InternalType))
        {
            throw new Exception(GetInvalidTypeErrorMsg(value));
        }
        
        compiler.Builder.BuildStore(value.LLVMValue, LLVMValue);
        return this;
    }
    
    public override Value DeRef(LLVMCompiler compiler) //TODO: might need adjusting for the new type engine
    {
        if (!InternalType.BaseType.Equals(Primitives.Void))
        {
            return Value.Create(InternalType.BaseType, compiler.Builder.BuildLoad2(InternalType.BaseType.LLVMType, LLVMValue));
        }
        else
        {
            throw new Exception("Cannot load pointer to void.");
        }
    }

    public virtual string GetInvalidTypeErrorMsg(Value value)
    {
        return $"Tried to assign value of type \"{value.InternalType}\" to pointer of type \"{InternalType.BaseType}\".";
    }
}

public class AspectPointer : Pointer
{
    public override AspectPtrType InternalType { get; }
    public Trait Trait { get => InternalType.BaseType; }

    public AspectPointer(AspectPtrType type, LLVMValueRef llvmValue) : base(type, llvmValue)
    {
        InternalType = type;
    }

    public Value CallMethod(LLVMCompiler compiler, AspectMethod method, Value[] args)
    {
        var index = InternalType.BaseType.VTable.GetIndex(method);
        var vtable = compiler.Builder.BuildExtractElement(LLVMValue, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1));
        var func = compiler.Builder.BuildInBoundsGEP2(method.InternalType.LLVMType, vtable, index);
        return method.InternalType.Call(compiler, func, args);
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