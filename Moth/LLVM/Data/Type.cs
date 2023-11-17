using Moth.AST.Node;
using Moth.LLVM.Data;

namespace Moth.LLVM.Data;

public enum TypeKind
{
    Class,
    Function,
    Pointer,
    Reference,
}

public abstract class Type : CompilerData
{
    public readonly LLVMTypeRef LLVMType;
    public readonly TypeKind Kind;


    public Type(LLVMTypeRef llvmType, TypeKind kind)
    {
        LLVMType = llvmType;
        Kind = kind;
    }

    public abstract override string ToString();
    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();
}