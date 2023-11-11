using Moth.LLVM.Data;

namespace Moth.LLVM;

public enum TypeKind
{
    Class,
    Function,
    Pointer,
    Reference,
}

public class Type
{
    public readonly LLVMTypeRef LLVMType;
    public readonly Class Class;
    public readonly TypeKind Kind;

    public Type(LLVMTypeRef llvmType, Class @class, TypeKind kind)
    {
        Kind = kind;
        LLVMType = llvmType;
        Class = @class;
    }

    public override string ToString() => Class.Name;

    public override bool Equals(object? obj)
        => obj is Type type
            && Class != null
            && type.Class != null
            && LLVMType.Kind == type.LLVMType.Kind
            && Kind == type.Kind
            && Class.Name == type.Class.Name;

    public override int GetHashCode() => Kind.GetHashCode() * Class.Name.GetHashCode() * (int)LLVMType.Kind;
}

public abstract class BasedType : Type
{
    public readonly Type BaseType;

    public BasedType(Type baseType, TypeKind kind)
        : base(LLVMTypeRef.CreatePointer(baseType.LLVMType, 0), baseType.Class, kind) => BaseType = baseType;

    public uint GetDepth()
    {
        Type? type = BaseType;
        uint depth = 0;

        while (type != null)
        {
            depth++;
            type = type is BasedType bType ? bType.BaseType : null;
        }

        return depth;
    }

    public override string ToString()
    {
        var builder = new StringBuilder(Class.Name);
        Type? type = BaseType;

        while (type != null)
        {
            builder.Append('*');
            type = type is BasedType bType ? bType.BaseType : null;
        }

        return builder.ToString();
    }

    public override bool Equals(object? obj) => base.Equals(obj) && obj is BasedType bType && BaseType.Equals(bType.BaseType);

    public override int GetHashCode() => base.GetHashCode() * BaseType.GetHashCode();
}

public sealed class FuncType : Type, ICallable
{
    public Type ReturnType { get; }
    public Type[] ParameterTypes { get; }

    public FuncType(Type retType, Type[] paramTypes, LLVMTypeRef llvmType)
        : base(llvmType, null, TypeKind.Function)
    {
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

    public override int GetHashCode() => base.GetHashCode() * ReturnType.GetHashCode();

    public Value Call(LLVMCompiler compiler, Function func, Value[] args)
    {
        return Equals(func.Type)
            ? new Value(ReturnType,
                compiler.Builder.BuildCall2(LLVMType,
                    func.LLVMValue,
                    args.AsLLVMValues(),
                    func.Name))
            : throw new Exception("Attempted to call a function with mismatched types. Critical failure, report ASAP.");
    }
}

public sealed class RefType : BasedType
{
    public RefType(Type baseType)
        : base(baseType, TypeKind.Reference) { }
}

public sealed class PtrType : BasedType
{
    public PtrType(Type baseType)
        : base(baseType, TypeKind.Pointer) { }
}