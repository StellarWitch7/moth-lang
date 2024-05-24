using Moth.AST.Node;
using Type = Moth.LLVM.Data.Type;

namespace Moth.LLVM.Data;

public class PtrType : InternalType
{
    public virtual InternalType BaseType { get; }

    protected PtrType(LLVMCompiler compiler, Type baseType, TypeKind kind)
        : base(compiler, LLVMTypeRef.CreatePointer(baseType.LLVMType, 0), kind)
    {
        if (baseType is VarType)
        {
            throw new Exception("Cannot create pointer to variable!");
        }
        
        BaseType = baseType;
    }
    
    public PtrType(LLVMCompiler compiler, Type baseType) : this(compiler, baseType, TypeKind.Pointer) { }

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

        var table = new ImplicitConversionTable(_compiler);
        
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

public class TraitPtrType : PtrType
{
    public override TraitDecl BaseType { get; }
    public override LLVMTypeRef LLVMType { get; }

    public TraitPtrType(LLVMCompiler compiler, TraitDecl baseType) : base(compiler, baseType, TypeKind.Pointer)
    {
        LLVMType = LLVMTypeRef.CreateStruct(new LLVMTypeRef[]
        {
            LLVMTypeRef.CreatePointer(_compiler.Int8.LLVMType, 0),
            LLVMTypeRef.CreatePointer(_compiler.Int8.LLVMType, 0)
        }, false);
    }
    
    public override bool Equals(object? obj) => obj is TraitPtrType && base.Equals(obj);
}

public class RefType : PtrType
{
    public RefType(LLVMCompiler compiler, Type baseType) : base(compiler, baseType, TypeKind.Reference)
    {
        if (baseType.Equals(_compiler.Void))
        {
            throw new Exception("Cannot create reference to void.");
        }
    }

    public override ImplicitConversionTable GetImplicitConversions()
    {
        var table = new ImplicitConversionTable(_compiler);
        
        table.Add(new PtrType(_compiler, BaseType), (prev) =>
        {
            return new Pointer(_compiler, new PtrType(_compiler, BaseType), prev.LLVMValue);
        });
        
        table.Add(BaseType, (prev) =>
        {
            return Value.Create(_compiler, BaseType, _compiler.Builder.BuildLoad2(BaseType.LLVMType, prev.LLVMValue));
        });

        return table;
    }

    public override string ToString() => $"{BaseType}&";

    public override bool Equals(object? obj) => obj is RefType && base.Equals(obj);
}

public sealed class VarType : RefType
{
    public VarType(LLVMCompiler compiler, Type baseType) : base(compiler, baseType) { }

    public override ImplicitConversionTable GetImplicitConversions()
    {
        var table = base.GetImplicitConversions();
        table.Remove(new PtrType(_compiler, BaseType));
        return table;
    }

    public override string ToString() => $"var {BaseType}";

    public override bool Equals(object? obj) => obj is VarType && base.Equals(obj);
}