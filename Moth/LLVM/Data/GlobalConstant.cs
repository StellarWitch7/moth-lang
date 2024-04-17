using Moth.AST.Node;

namespace Moth.LLVM.Data;

public sealed class GlobalConstant : ConstVar, IGlobal
{
    public Namespace Parent { get; }
    public PrivacyType Privacy { get; }
    
    public GlobalConstant(Namespace parent, string name, VarType type, LLVMValueRef llvmVariable, PrivacyType privacy)
        : base(name, type, llvmVariable)
    {
        Parent = parent;
        Privacy = privacy;
    }
}
