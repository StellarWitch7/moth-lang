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

    public Value ImplicitConvertTo(LLVMCompiler compiler, Type target)
    {
        if (Type.Equals(target))
        {
            return this;
        }

        var implicits = Type.GetImplicitConversions();

        if (implicits.TryGetValue(target, out Func<LLVMCompiler, Value, Value> convert))
        {
            return convert(compiler, this);
        }
        else
        {
            throw new Exception($"Cannot implicitly convert value of type \"{target}\" to \"{Type}\".");
        }
    }
    
    public virtual Value SafeLoad(LLVMCompiler compiler)
    {
        return this;
    }

    public virtual Pointer GetPointer(LLVMCompiler compiler)
    {
        LLVMValueRef newVal = compiler.Builder.BuildAlloca(Type.LLVMType);
        compiler.Builder.BuildStore(LLVMValue, newVal);
        return new Pointer(new PtrType(Type), newVal);
    }

    public virtual Value DeRef(LLVMCompiler compiler)
    {
        throw new Exception("Cannot dereference value as it is not a pointer!");
    }

    public static Value Create(Type type, LLVMValueRef value)
    {
        if (type is FuncType fnT)
        {
            return new Function(fnT, value, new Parameter[0]);
        }
        else if (type is PtrType ptrType)
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