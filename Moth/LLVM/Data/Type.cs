using Moth.AST.Node;
using Moth.LLVM.Data;
using System.Linq.Expressions;

namespace Moth.LLVM.Data;

public enum TypeKind
{
    Struct,
    Function,
    Pointer,
    Reference,
    Array
}

public class Type : CompilerData
{
    public readonly LLVMTypeRef LLVMType;
    public readonly TypeKind Kind;

    public Type(LLVMTypeRef llvmType, TypeKind kind)
    {
        LLVMType = llvmType;
        Kind = kind;
    }
    
    public virtual uint Bits
    {
        get
        {
            throw new NotImplementedException();
        }
    }
    
    public bool CanConvertTo(Type other)
    {
        if (Equals(other)) return true;
        return GetImplicitConversions().Contains(other);
    }
    
    public virtual ImplicitConversionTable GetImplicitConversions() => new ImplicitConversionTable();
    
    public override string ToString() => throw new NotImplementedException();

    public override bool Equals(object? obj) => throw new NotImplementedException();

    public override int GetHashCode() => (int)Kind;
}