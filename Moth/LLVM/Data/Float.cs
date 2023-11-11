using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Float : Class
{
    public static readonly Float Float16 = new Float(Reserved.Float16, LLVMTypeRef.Half, PrivacyType.Public);
    public static readonly Float Float32 = new Float(Reserved.Float32, LLVMTypeRef.Float, PrivacyType.Public);
    public static readonly Float Float64 = new Float(Reserved.Float64, LLVMTypeRef.Double, PrivacyType.Public);

    public Float(string name, LLVMTypeRef llvmType, PrivacyType privacy) : base(null, name, llvmType, privacy)
    {
    }
}
