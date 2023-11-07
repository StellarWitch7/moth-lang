using LLVMSharp.Interop;
using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class SignedInt : Int
{
    public SignedInt(string name, LLVMTypeRef llvmType, PrivacyType privacy) : base(name, llvmType, privacy)
    {
    }
}
