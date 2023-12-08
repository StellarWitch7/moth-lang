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

public class Type : CompilerData
{
    public readonly LLVMTypeRef LLVMType;
    public readonly TypeKind Kind;

    public Type(LLVMTypeRef llvmType, TypeKind kind)
    {
        LLVMType = llvmType;
        Kind = kind;
    }

    public override string ToString() => throw new NotImplementedException();

    public override bool Equals(object? obj) => throw new NotImplementedException();

    public override int GetHashCode() => throw new NotImplementedException();
}