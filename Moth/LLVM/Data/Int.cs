using Moth.AST.Node;

namespace Moth.LLVM.Data;

public abstract class Int : PrimitiveType
{
    protected Int(string name, LLVMTypeRef llvmType) : base(name, llvmType) { }
}

public sealed class SignedInt : Int
{
    public SignedInt(string name, LLVMTypeRef llvmType) : base(name, llvmType) { }
}

public sealed class UnsignedInt : Int
{
    public UnsignedInt(string name, LLVMTypeRef llvmType) : base(name, llvmType) { }
}

