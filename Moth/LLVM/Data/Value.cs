namespace Moth.LLVM.Data;

public class Value : ICompilerData
{
    public bool IsExternal { get; init; }
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

        if (Type is VarType)
        {
            return DeRef(compiler).ImplicitConvertTo(compiler, target);
        }

        var implicits = Type.GetImplicitConversions();

        if (implicits.TryGetValue(target, out Func<LLVMCompiler, Value, Value> convert))
        {
            return convert(compiler, this);
        }
        else
        {
            throw new Exception($"Cannot implicitly convert value of type \"{Type}\" to \"{target}\".");
        }
    }

    public Pointer GetRef(LLVMCompiler compiler)
    {
        if (Type is VarType)
        {
            return DeRef(compiler).GetRef(compiler);
        }
        
        LLVMValueRef newVal = compiler.Builder.BuildAlloca(Type.LLVMType);
        compiler.Builder.BuildStore(LLVMValue, newVal);
        return new Pointer(new RefType(Type), newVal);
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
        else if (type is AspectPtrType aspPtrType)
        {
            return new AspectPointer(aspPtrType, value);
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