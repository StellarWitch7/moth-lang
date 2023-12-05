namespace Moth.LLVM.Data;

public class PtrType : Type
{
    public readonly Type BaseType;

    public PtrType(Type baseType, TypeKind kind)
        : base(LLVMTypeRef.CreatePointer(baseType.LLVMType, 0), kind) => BaseType = baseType;

    public uint GetDepth()
    {
        Type? type = BaseType;
        uint depth = 0;

        while (type != null)
        {
            depth++;
            type = type is PtrType bType ? bType.BaseType : null;
        }

        return depth;
    }

    public override string ToString() => BaseType + "*";

    public override bool Equals(object? obj) => obj is PtrType bType && BaseType.Equals(bType.BaseType);

    public override int GetHashCode() => BaseType.GetHashCode();
}

public sealed class RefType : PtrType
{
    public RefType(Type baseType)
        : base(baseType, TypeKind.Reference) { }
}