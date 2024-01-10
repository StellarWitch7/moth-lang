using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class PtrType : Type
{
    public virtual Type BaseType { get; }

    protected PtrType(Type baseType, TypeKind kind)
        : base(LLVMTypeRef.CreatePointer(baseType.LLVMType, 0), kind) => BaseType = baseType;
    
    public PtrType(Type baseType) : this(baseType, TypeKind.Pointer) { }

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

    public override uint Bits
    {
        get
        {
            return 32;
        }
    }

    public override string ToString() => BaseType + "*";

    public override bool Equals(object? obj) => obj is PtrType bType && BaseType.Equals(bType.BaseType);

    public override int GetHashCode() => BaseType.GetHashCode();
}

public sealed class RefType : PtrType
{
    public RefType(Type baseType) : base(baseType, TypeKind.Reference) { }
}