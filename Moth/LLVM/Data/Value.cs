namespace Moth.LLVM.Data;

public class Value : ICompilerData
{
    public bool IsExternal { get; init; }
    public virtual Type Type { get; }
    public virtual LLVMValueRef LLVMValue { get; }

    protected LLVMCompiler _compiler { get; }

    protected Value(LLVMCompiler compiler, Type type, LLVMValueRef value)
    {
        _compiler = compiler;
        Type = type;
        LLVMValue = value;
    }

    public Value ImplicitConvertTo(Type target)
    {
        if (Type.Equals(target))
        {
            return this;
        }

        if (Type is VarType)
        {
            return DeRef().ImplicitConvertTo(target);
        }

        var implicits = Type.GetImplicitConversions();

        if (implicits.TryGetValue(target, out Func<Value, Value> convert))
        {
            return convert(this);
        }
        else
        {
            throw new Exception(
                $"Cannot implicitly convert value of type \"{Type}\" to \"{target}\"."
            );
        }
    }

    public Pointer GetRef()
    {
        if (Type is VarType)
        {
            return DeRef().GetRef();
        }

        LLVMValueRef newVal = _compiler.Builder.BuildAlloca(Type.LLVMType);
        _compiler.Builder.BuildStore(LLVMValue, newVal);
        return new Pointer(_compiler, new RefType(_compiler, Type), newVal);
    }

    public virtual Value DeRef()
    {
        throw new Exception("Cannot dereference value as it is not a pointer!");
    }

    public static Value Create(LLVMCompiler compiler, Type type, LLVMValueRef value)
    {
        if (type is FuncType fnT)
        {
            return new Function(compiler, fnT, value, new Parameter[0]);
        }
        else if (type is TraitPtrType aspPtrType)
        {
            return new TraitPointer(compiler, aspPtrType, value);
        }
        else if (type is PtrType ptrType)
        {
            return new Pointer(compiler, ptrType, value);
        }
        else
        {
            return new Value(compiler, type, value);
        }
    }

    public static Pointer CreatePtrToTemp(LLVMCompiler compiler, Value temporary)
    {
        var tempPtr = compiler.Builder.BuildAlloca(temporary.Type.LLVMType);
        compiler.Builder.BuildStore(temporary.LLVMValue, tempPtr);
        return new Pointer(compiler, new PtrType(compiler, temporary.Type), tempPtr);
    }
}
