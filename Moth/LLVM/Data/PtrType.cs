using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class PtrType : Type
{
    public virtual Type BaseType { get; }

    protected PtrType(Type baseType, TypeKind kind)
        : base(LLVMTypeRef.CreatePointer(baseType.LLVMType, 0), kind)
    {
        if (baseType is RefType)
        {
            throw new Exception("References cannot be pointed to or referenced!");
        }
        
        BaseType = baseType;
    }
    
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

    public override string ToString() => $"{BaseType}*";

    public override bool Equals(object? obj) => obj is PtrType bType && BaseType.Equals(bType.BaseType);

    public override int GetHashCode() => BaseType.GetHashCode();
}

public sealed class RefType : PtrType
{
    public RefType(Type baseType) : base(baseType, TypeKind.Reference)
    {
        if (baseType.Equals(Primitives.Void))
        {
            throw new Exception("Cannot create reference to void.");
        }
    }

    public override Dictionary<Type, Func<LLVMCompiler, Value, Value>> GetImplicitConversions()
    {
        var dict = new Dictionary<Type, Func<LLVMCompiler, Value, Value>>();
        
        dict.Add(new PtrType(BaseType), (compiler, prev) =>
        {
            return new Pointer(new PtrType(BaseType), prev.LLVMValue);
        });
        
        dict.Add(BaseType, (compiler, prev) =>
        {
            return Value.Create(BaseType, compiler.Builder.BuildLoad2(BaseType.LLVMType, prev.LLVMValue));
        });

        return dict;
    }

    public override string ToString() => $"{BaseType}&";
}