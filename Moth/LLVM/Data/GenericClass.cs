using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class GenericClass : Class
{
    public Dictionary<string, Type> TypeParams { get; set; } = new Dictionary<string, Type>();

    public GenericClass(string name, LLVMTypeRef llvmType, PrivacyType privacy) : base(name, llvmType, privacy)
    {
    }
}
