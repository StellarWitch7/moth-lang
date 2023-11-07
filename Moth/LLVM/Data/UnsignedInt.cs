using LLVMSharp.Interop;
using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class UnsignedInt : Int
{
    public UnsignedInt(string name, LLVMTypeRef llvmType, PrivacyType privacy) : base(name, llvmType, privacy)
    {
    }
}
