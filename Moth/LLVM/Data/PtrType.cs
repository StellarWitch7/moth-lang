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

    public override string ToString() => BaseType + "*";

    public override bool Equals(object? obj) => obj is PtrType bType && BaseType.Equals(bType.BaseType);

    public override int GetHashCode() => BaseType.GetHashCode();
}

public sealed class RefType : PtrType
{
    public RefType(Type baseType) : base(baseType, TypeKind.Reference) { }
}

public sealed class ArrType : PtrType
{
    public override PrimitiveType BaseType { get; }
    public Type ElementType { get; }
    
    public ArrType(LLVMCompiler compiler, Type elementType)
        : base(new PrimitiveType($"[{elementType}]",
            compiler.Context.GetStructType(new []
            {
                new PtrType(elementType).LLVMType,
                LLVMTypeRef.Int32
                
            }, false)))
    {
        if (base.BaseType is not PrimitiveType primType)
        {
            throw new Exception("How in all hell did you manage that. (Critical compiler error, report to dev ASAP.)");
        }
        
        BaseType = primType;
        BaseType.Fields.Add("Length", new Field("Length", 1, Primitives.UInt32, PrivacyType.Public));
        BaseType.Methods.Add(new Signature(Reserved.Indexer, new Type[] { new PtrType(BaseType), Primitives.UInt32 }),
            new ArrayIndexerFunction(compiler, BaseType, elementType));
        ElementType = elementType;
    }

    public override string ToString() => $"#[{ElementType}]";

    public override bool Equals(object? obj)
    {
        if (obj is not ArrType arrType)
        {
            return false;
        }

        if (!ElementType.Equals(arrType.ElementType))
        {
            return false;
        }

        return base.Equals(obj);
    }

    public override int GetHashCode() => base.GetHashCode() + ElementType.GetHashCode();
}