using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class PrimitiveType : Struct
{
    internal PrimitiveType(string name, LLVMTypeRef llvmType) : base(null, name, llvmType, PrivacyType.Public) { }

    public override string FullName
    {
        get
        {
            return $"#{Name}";
        }
    }
}
