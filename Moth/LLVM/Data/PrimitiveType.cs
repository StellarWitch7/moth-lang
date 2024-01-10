using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class PrimitiveType : Struct
{
    private uint _bitlength;
    
    internal PrimitiveType(string name, LLVMTypeRef llvmType, uint bitlength)
        : base(null, name, llvmType, PrivacyType.Public)
    {
        _bitlength = bitlength;
    }

    public override string FullName
    {
        get
        {
            return $"#{Name}";
        }
    }

    public override uint Bits
    {
        get
        {
            return _bitlength;
        }
    }
}

public sealed class ArrType : PrimitiveType
{
    public Type ElementType { get; }
    
    public ArrType(LLVMCompiler compiler, Type elementType)
        : base($"[{elementType}]",
            compiler.Context.GetStructType(new []
            {
                new PtrType(elementType).LLVMType,
                LLVMTypeRef.Int32
                
            }, false), 64)
    {
        Fields.Add("Length", new Field("Length", 1, Primitives.UInt32, PrivacyType.Public));
        Methods.Add(new Signature(Reserved.Indexer, new Type[]
            {
                new PtrType(this),
                Primitives.UInt32
            }),
            new ArrayIndexerFunction(compiler, this, elementType));
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