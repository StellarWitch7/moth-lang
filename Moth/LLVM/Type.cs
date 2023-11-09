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

    public override string ToString() => Class.Name;

    public override bool Equals(object? obj) => obj is Type type && LLVMType.Kind == type.LLVMType.Kind && Class.Name == type.Class.Name && Kind == type.Kind;

    public override int GetHashCode() => Kind.GetHashCode() * Class.Name.GetHashCode() * (int)LLVMType.Kind;
}

public abstract class BasedType : Type
{
    public readonly Type BaseType;

    public BasedType(Type baseType, LLVMTypeRef llvmType, Class classOfType, TypeKind kind)
        : base(llvmType, classOfType, kind) => BaseType = baseType;

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

public sealed class RefType : BasedType
{
    public RefType(Type baseType, LLVMTypeRef llvmType, Class classOfType)
        : base(baseType, llvmType, classOfType, TypeKind.Reference) { }
}

public sealed class PtrType : BasedType
{
    public PtrType(Type baseType, LLVMTypeRef llvmType, Class classOfType)
        : base(baseType, llvmType, classOfType, TypeKind.Pointer) { }
}