namespace Moth.LLVM.Data;

public class BasedType : Type
{
    public readonly Type BaseType;

    public BasedType(Type baseType, TypeKind kind)
        : base(LLVMTypeRef.CreatePointer(baseType.LLVMType, 0), kind) => BaseType = baseType;

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

    public override string ToString() => BaseType + "*";

    public override bool Equals(object? obj) => obj is BasedType bType && BaseType.Equals(bType.BaseType);

    public override int GetHashCode() => BaseType.GetHashCode();
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