using Moth.AST.Node;
using Moth.LLVM.Data;

namespace Moth.LLVM.Data;

public enum TypeKind
{
    Class,
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
        foreach (var key in GetImplicitConversions().Keys)
        {
            if (key.Equals(other))
            {
                return true;
            }
        }

        return false;
    }
    
    public virtual Dictionary<Type, Func<LLVMCompiler, Value, Value>> GetImplicitConversions() => throw new NotImplementedException();
    
    public override string ToString() => throw new NotImplementedException();

    public override bool Equals(object? obj) => throw new NotImplementedException();

    public override int GetHashCode() => (int)Kind;
}