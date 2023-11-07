using LLVMSharp.Interop;
using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Int : Class
{
    public Int(string name, LLVMTypeRef llvmType, PrivacyType privacy) : base(name, llvmType, privacy)
    {
    }
}
