namespace Moth.LLVM.Data;

public class FuncType : PtrType
{
    public string Name { get; }
    public Type ReturnType { get; }
    public Type[] ParameterTypes { get; }
    public bool IsVariadic { get; }
    public Struct? OwnerStruct { get; }

    public FuncType(string name, Type retType, Type[] paramTypes, bool isVariadic, Struct? ownerStruct)
        : base(new Type(LLVMTypeRef.CreateFunction(retType.LLVMType, paramTypes.AsLLVMTypes(), isVariadic), TypeKind.Function))
    {
        Name = name;
        ReturnType = retType;
        ParameterTypes = paramTypes;
        IsVariadic = isVariadic;
        OwnerStruct = ownerStruct;
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

        if (IsVariadic != fnType.IsVariadic)
        {
            return false;
        }

        if (OwnerStruct != fnType.OwnerStruct)
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
        => Value.Create(ReturnType, compiler.Builder.BuildCall2(BaseType.LLVMType,
            func,
            args.AsLLVMValues(),
            name));
}

public sealed class MethodType : FuncType
{
    public bool IsStatic { get; }
    
    public MethodType(string name, Type retType, Type[] paramTypes, Struct ownerStruct, bool isStatic = false)
        : base(name, retType, paramTypes, false, ownerStruct)
    {
        IsStatic = isStatic;
        
        if (ownerStruct == null)
        {
            throw new Exception($"Could not create new method \"{name}\" because methods must have a non-null parent struct!");
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is not MethodType methodType)
        {
            return false;
        }

        if (IsStatic != methodType.IsStatic)
        {
            return false;
        }

        return base.Equals(methodType);
    }
}

public sealed class LocalFuncType : FuncType
{
    public LocalFuncType(Type retType, Type[] paramTypes)
        : base(Reserved.LocalFunc, retType, paramTypes, false, null) { }
}