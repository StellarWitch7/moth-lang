namespace Moth.LLVM.Data;

public class Value : CompilerData
{
    public virtual Type Type { get; }
    public virtual LLVMValueRef LLVMValue { get; }

    protected Value(Type type, LLVMValueRef value)
    {
        Type = type;
        LLVMValue = value;
    }
    
    public virtual Value SafeLoad(LLVMCompiler compiler)
    {
        return this;
    }

    public virtual Pointer GetAddr(LLVMCompiler compiler)
    {
        LLVMValueRef newVal = compiler.Builder.BuildAlloca(Type.LLVMType);
        compiler.Builder.BuildStore(LLVMValue, newVal);
        return new Pointer(new PtrType(Type), newVal);
    }

    public static Value Create(Type type, LLVMValueRef value)
    {
        if (type is PtrType ptrType)
        {
            return new Pointer(ptrType, value);
        }
        else
        {
            return new Value(type, value);
        }
    }

    public static Pointer CreatePtrToTemp(LLVMCompiler compiler, Value temporary)
    {
        var tempPtr = compiler.Builder.BuildAlloca(temporary.Type.LLVMType);
        compiler.Builder.BuildStore(temporary.LLVMValue, tempPtr);
        return new Pointer(new PtrType(temporary.Type), tempPtr);
    }
}

public class ClassValue : Value //TODO: does this need to exist
{
    public ClassValue(Class type, LLVMValueRef value) : base(type, value) { }
}

public class FieldValue : Value //TODO: owo, what's this?
{
    public FieldValue(LLVMCompiler compiler, Field field, ClassValue owner)
        : base(field.Type,
            compiler.Builder.BuildStructGEP2(owner.Type.LLVMType,
                owner.LLVMValue,
                field.FieldIndex,
                field.Name)) { }
}