using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Float : Struct
{
    public Float(string name, LLVMTypeRef llvmType, PrivacyType privacy) : base(null, name, llvmType, privacy)
    {
    }
}
