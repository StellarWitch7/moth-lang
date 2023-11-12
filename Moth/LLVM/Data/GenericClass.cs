using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class GenericClass : Class
{
    public Dictionary<string, ClassType> TypeParams { get; set; } = new Dictionary<string, ClassType>();

    public GenericClass(IContainer? parent, string name, LLVMTypeRef llvmType, PrivacyType privacy)
        : base(parent, name, llvmType, privacy)
    {
    }
}
