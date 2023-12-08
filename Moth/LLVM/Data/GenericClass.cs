using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class GenericClass : Class
{
    public Dictionary<string, Type> TypeParams { get; set; } = new Dictionary<string, Type>();

    public GenericClass(Namespace? parent, string name, LLVMTypeRef llvmType, PrivacyType privacy)
        : base(parent, name, llvmType, privacy)
    {
    }
}
