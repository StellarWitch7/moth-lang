using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Class : Struct
{
    public Class(Namespace? parent, string name, LLVMTypeRef llvmType, PrivacyType privacy)
        : base(parent, name, llvmType, privacy) { }
}
