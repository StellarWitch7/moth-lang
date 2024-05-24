namespace Moth.LLVM.Data;

public class FuncType : PtrType
{
    public Type ReturnType { get; }
    public Type[] ParameterTypes { get; }
    public bool IsVariadic { get; }

    public FuncType(Type retType, Type[] paramTypes, bool isVariadic)
        : base(new Type(LLVMTypeRef.CreateFunction(retType.LLVMType, paramTypes.AsLLVMTypes(), isVariadic), TypeKind.Function))
    {
        ReturnType = retType;
        ParameterTypes = paramTypes;
        IsVariadic = isVariadic;
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

    public override string ToString()
    {
        var builder = new StringBuilder("(");
        
        foreach (var type in ParameterTypes)
        {
            builder.Append($"{type}, ");
        }

        if (ParameterTypes.Length > 0)
        {
            builder.Remove(builder.Length - 2, 2);
        }

        builder.Append(')');
        return builder.ToString();
    }

    public override int GetHashCode() => ReturnType.GetHashCode() * ParameterTypes.GetHashes();

    public virtual Value Call(LLVMCompiler compiler, LLVMValueRef func, Value[] args)
        => Value.Create(ReturnType, compiler.Builder.BuildCall2(BaseType.LLVMType, func, args.AsLLVMValues()));
}

public sealed class MethodType : FuncType
{
    public TypeDecl OwnerTypeDecl { get; }
    public bool IsStatic { get; }
    
    public MethodType(Type retType, Type[] paramTypes, TypeDecl ownerTypeDecl, bool isStatic = false)
        : base(retType, paramTypes, false)
    {
        OwnerTypeDecl = ownerTypeDecl;
        IsStatic = isStatic;
        
        if (ownerTypeDecl == null)
        {
            throw new Exception($"Could not create new method because methods must have a non-null parent struct!");
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

        if (!OwnerTypeDecl.Equals(methodType.OwnerTypeDecl))
        {
            return false;
        }

        return base.Equals(methodType);
    }
}

public sealed class LocalFuncType : FuncType
{
    public LocalFuncType(Type retType, Type[] paramTypes)
        : base(retType, paramTypes, false) { }
}