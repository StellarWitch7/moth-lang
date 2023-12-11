using Moth.AST.Node;

namespace Moth.LLVM.Data;

public sealed class GlobalConstant : ConstVar, IGlobal
{
    public PrivacyType Privacy { get; }
    
    public GlobalConstant(string name, RefType type, LLVMValueRef llvmVariable, PrivacyType privacy)
        : base(name, type, llvmVariable)
    {
        Privacy = privacy;
    }
}
