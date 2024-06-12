using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Pointer : Value
{
    public override PtrType Type { get; }

    public Pointer(LLVMCompiler compiler, PtrType type, LLVMValueRef llvmValue)
        : base(compiler, null, llvmValue)
    {
        Type = type;
    }

    public virtual Pointer Store(Value value)
    {
        if (!Type.BaseType.Equals(value.Type))
        {
            throw new Exception(GetInvalidTypeErrorMsg(value));
        }

        _compiler.Builder.BuildStore(value.LLVMValue, LLVMValue);
        return this;
    }

    public override Value DeRef() //TODO: might need adjusting for the new type engine
    {
        if (!Type.BaseType.Equals(_compiler.Void))
        {
            return Value.Create(
                _compiler,
                Type.BaseType,
                _compiler.Builder.BuildLoad2(Type.BaseType.LLVMType, LLVMValue)
            );
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

public class TraitPointer : Pointer
{
    public override TraitPtrType Type { get; }
    public TraitDecl TraitDecl
    {
        get => Type.BaseType;
    }

    public TraitPointer(LLVMCompiler compiler, TraitPtrType type, LLVMValueRef llvmValue)
        : base(compiler, type, llvmValue)
    {
        Type = type;
    }

    public Value CallMethod(LLVMCompiler compiler, AspectMethod method, Value[] args)
    {
        var index = Type.BaseType.VTable.GetIndex(method);
        var vtable = compiler.Builder.BuildExtractElement(
            LLVMValue,
            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1)
        );
        var func = compiler.Builder.BuildInBoundsGEP2(method.Type.LLVMType, vtable, index);
        return method.Type.Call(func, args);
    }

    public override Pointer Store(Value value)
    {
        throw new NotImplementedException(); //TODO: this is an illegal function to call
    }

    public override Value DeRef()
    {
        throw new NotImplementedException(); //TODO: this is an illegal function to call
    }
}
