using LLVMSharp.Interop;
using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Float : Class
{
    public Float(string name, LLVMTypeRef llvmType, PrivacyType privacy) : base(name, llvmType, privacy)
    {
    }
}
