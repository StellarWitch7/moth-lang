using LLVMSharp.Interop;
using Moth.LLVM.Data;

namespace Moth.LLVM;

public enum TypeKind
{
    Class,
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

    public override string ToString()
    {
        return Class.Name;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Type type)
            return false;

        if (LLVMType.Kind != type.LLVMType.Kind)
            return false;

        if (Class.Name != type.Class.Name)
            return false;

        if (Kind != type.Kind)
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        return Kind.GetHashCode() * Class.Name.GetHashCode() * (int)LLVMType.Kind;
    }
}

public abstract class BasedType : Type
{
    public readonly Type BaseType;

    public BasedType(Type baseType, LLVMTypeRef llvmType, Class classOfType, TypeKind kind) 
        : base(llvmType, classOfType, kind)
    {
        BaseType = baseType;
    }

    public uint GetDepth()
    {
        var type = BaseType;
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
        StringBuilder builder = new StringBuilder(Class.Name);
        var type = BaseType;

        while (type != null)
        {
            builder.Append('*');
            type = type is BasedType bType ? bType.BaseType : null;
        }

        return builder.ToString();
    }

    public override bool Equals(object? obj)
    {
        if (!base.Equals(obj))
        {
            return false;
        }

        if (obj is BasedType bType)
        {
            return BaseType.Equals(bType.BaseType);
        }
        else
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        return base.GetHashCode() * BaseType.GetHashCode();
    }
}

public sealed class RefType : BasedType
{
    public RefType(Type baseType, LLVMTypeRef llvmType, Class classOfType) 
        : base(baseType, llvmType, classOfType, TypeKind.Reference) {}
}

public sealed class PtrType : BasedType
{
    public PtrType(Type baseType, LLVMTypeRef llvmType, Class classOfType) 
        : base(baseType, llvmType, classOfType, TypeKind.Pointer) {}
}