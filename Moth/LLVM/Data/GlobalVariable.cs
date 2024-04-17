using Moth.AST.Node;

namespace Moth.LLVM.Data;

public sealed class GlobalVariable : Variable, IGlobal
{
    public Namespace Parent { get; }
    public PrivacyType Privacy { get; }
    
    public GlobalVariable(Namespace parent, string name, VarType type, LLVMValueRef llvmVariable, PrivacyType privacy)
        : base(name, type, llvmVariable)
    {
        Parent = parent;
        Privacy = privacy;
    }
}
