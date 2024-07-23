using System.Linq.Expressions;
using Moth.AST;
using Moth.AST.Node;
using Moth.LLVM.Data;

namespace Moth.LLVM.Data;

public enum TypeKind
{
    Decl,
    Function,
    Pointer,
    Reference,
    Array
}

public class Type : ICompilerData
{
    public bool IsExternal { get; init; }
    public IASTNode? Node { get; init; }
    public virtual LLVMTypeRef LLVMType { get; }
    public virtual TypeKind Kind { get; }

    protected LLVMCompiler _compiler { get; }

    public Type(LLVMCompiler compiler, LLVMTypeRef llvmType, TypeKind kind)
    {
        _compiler = compiler;
        LLVMType = llvmType;
        Kind = kind;
    }

    public virtual uint Bits
    {
        get { throw new NotImplementedException(); }
    }

    public bool CanConvertTo(Type other)
    {
        if (Equals(other))
            return true;
        return GetImplicitConversions().Contains(other);
    }

    public virtual ImplicitConversionTable GetImplicitConversions() =>
        new ImplicitConversionTable(_compiler);

    public override string ToString() => throw new NotImplementedException();

    public override bool Equals(object? obj) => throw new NotImplementedException();

    public override int GetHashCode() => (int)Kind;
}
