using Moth.AST.Node;

namespace Moth.LLVM.Data;

public abstract class Int : Struct
{
    protected Int(string name, LLVMTypeRef llvmType, PrivacyType privacy) : base(null, name, llvmType, privacy) { }
}

public sealed class SignedInt : Int
{
    public SignedInt(string name, LLVMTypeRef llvmType, PrivacyType privacy) : base(name, llvmType, privacy) { }
}

public sealed class UnsignedInt : Int
{
    public UnsignedInt(string name, LLVMTypeRef llvmType, PrivacyType privacy) : base(name, llvmType, privacy) { }
}

