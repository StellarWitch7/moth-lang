using Moth.AST.Node;
using System.Runtime.InteropServices;

namespace Moth.LLVM.Data;

public class PtrType : InternalType
{
    public virtual InternalType BaseType { get; }

    protected PtrType(InternalType baseType, TypeKind kind)
        : base(LLVMTypeRef.CreatePointer(baseType.LLVMType, 0), kind)
    {
        if (baseType is VarType)
        {
            throw new Exception("Cannot create pointer to variable!");
        }
        
        BaseType = baseType;
    }
    
    public PtrType(InternalType baseType) : this(baseType, TypeKind.Pointer) { }

    public uint GetDepth()
    {
        InternalType? type = BaseType;
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
            return (uint)(IntPtr.Size * 8); //TODO: this might be very bad
        }
    }

    public override ImplicitConversionTable GetImplicitConversions() //TODO: no implicit casting between pointer types
    {
        // if (BaseType.Equals(Primitives.Void))
        // {
        //     return new Void.ImplicitConversionTable();
        // }

        var table = new ImplicitConversionTable();
        
        // table.Add(new PtrType(Primitives.Void), (compiler, prev) =>
        // {
        //     return new Pointer(new PtrType(Primitives.Void), prev.LLVMValue);
        // });
        
        return table;
    }
    
    public override string ToString() => $"{BaseType}*";

    public override bool Equals(object? obj) => obj is PtrType bType && BaseType.Equals(bType.BaseType);

    public override int GetHashCode() => BaseType.GetHashCode();
}

public class AspectPtrType : PtrType
{
    public override TraitDecl BaseType { get; }
    public override LLVMTypeRef LLVMType { get => llvmType; }
    
    private static LLVMTypeRef llvmType = LLVMTypeRef.CreateStruct(new LLVMTypeRef[]
    {
        LLVMTypeRef.CreatePointer(Primitives.Int8.LLVMType, 0),
        LLVMTypeRef.CreatePointer(Primitives.Int8.LLVMType, 0)
    }, false);

    public AspectPtrType(TraitDecl baseType) : base(baseType, TypeKind.Pointer) { }
    
    public override bool Equals(object? obj) => obj is AspectPtrType && base.Equals(obj);
}

public class RefType : PtrType
{
    public RefType(InternalType baseType) : base(baseType, TypeKind.Reference)
    {
        if (baseType.Equals(Primitives.Void))
        {
            throw new Exception("Cannot create reference to void.");
        }
    }

    public override ImplicitConversionTable GetImplicitConversions()
    {
        var table = new ImplicitConversionTable();
        
        table.Add(new PtrType(BaseType), (compiler, prev) =>
        {
            return new Pointer(new PtrType(BaseType), prev.LLVMValue);
        });
        
        table.Add(BaseType, (compiler, prev) =>
        {
            return Value.Create(BaseType, compiler.Builder.BuildLoad2(BaseType.LLVMType, prev.LLVMValue));
        });

        return table;
    }

    public override string ToString() => $"{BaseType}&";

    public override bool Equals(object? obj) => obj is RefType && base.Equals(obj);
}

public sealed class VarType : RefType
{
    public VarType(InternalType baseType) : base(baseType) { }

    public override ImplicitConversionTable GetImplicitConversions()
    {
        var table = base.GetImplicitConversions();
        table.Remove(new PtrType(BaseType));
        return table;
    }

    public override string ToString() => $"var {BaseType}";

    public override bool Equals(object? obj) => obj is VarType && base.Equals(obj);
}