namespace Moth.LLVM.Data;

public class FuncType : PtrType
{
    public readonly string Name;
    public Type ReturnType { get; }
    public Type[] ParameterTypes { get; }

    public FuncType(string name, Type retType, Type[] paramTypes)
        : base(new Type(LLVMTypeRef.CreateFunction(retType.LLVMType, paramTypes.AsLLVMTypes()), TypeKind.Function))
    {
        Name = name;
        ReturnType = retType;
        ParameterTypes = paramTypes;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not FuncType fnType)
        {
            return false;
        }

        if (!ReturnType.Equals(fnType.ReturnType))
        {
            return false;
        }

        if (ParameterTypes.Length != fnType.ParameterTypes.Length)
        {
            return false;
        }

        uint index = 0;

        foreach (Type param in ParameterTypes)
        {
            if (!param.Equals(fnType.ParameterTypes[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    public override int GetHashCode() => Name.GetHashCode() * ParameterTypes.GetHashes();

    public Value Call(LLVMCompiler compiler, LLVMValueRef func, Value[] args)
    {
        return Call(compiler, func, args, ReturnType.LLVMType.Kind != LLVMTypeKind.LLVMVoidTypeKind
            ? Name
            : "");
    }

    public virtual Value Call(LLVMCompiler compiler, LLVMValueRef func, Value[] args, string name)
        => new Value(ReturnType, compiler.Builder.BuildCall2(BaseType.LLVMType,
            func,
            args.AsLLVMValues(),
            name));
}

public class LLVMFuncType : FuncType
{
    public readonly bool IsVariadic;

    public LLVMFuncType(string name, Type retType, Type[] paramTypes, bool isVariadic)
        : base(name, retType, paramTypes)
    {
        IsVariadic = isVariadic;
    }

    public Struct? OwnerClass
    {
        get
        {
            return this is MethodType defFunc
                ? defFunc.OwnerStruct
                : null;
        }
    }

    public override string ToString() => throw new NotImplementedException();
}

public sealed class MethodType : LLVMFuncType
{
    public new Struct? OwnerStruct { get; set; }

    public MethodType(string name, Type retType, Type[] paramTypes, bool isVariadic, Struct? ownerStruct)
        : base(name, retType, paramTypes, isVariadic)
    {
        OwnerStruct = ownerStruct;
    }
}

public sealed class LocalFuncType : LLVMFuncType
{
    public LocalFuncType(Type retType, Type[] paramTypes)
        : base(Reserved.LocalFunc, retType, paramTypes, false) { }
}