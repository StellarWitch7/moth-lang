using Moth.AST.Node;

namespace Moth.LLVM.Data;

public sealed class GlobalVariable : Variable, IGlobal
{
    public PrivacyType Privacy { get; }
    
    public GlobalVariable(string name, VarType type, LLVMValueRef llvmVariable, PrivacyType privacy)
        : base(name, type, llvmVariable)
    {
        Privacy = privacy;
    }
}
