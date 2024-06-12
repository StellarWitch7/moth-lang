using Moth.AST.Node;

namespace Moth.LLVM.Data;

public sealed class GlobalConstant : ConstVar, IGlobal
{
    public Namespace Parent { get; }
    public Dictionary<string, IAttribute> Attributes { get; }
    public PrivacyType Privacy { get; }

    public GlobalConstant(
        LLVMCompiler compiler,
        Namespace parent,
        string name,
        VarType type,
        LLVMValueRef llvmVariable,
        Dictionary<string, IAttribute> attributes,
        PrivacyType privacy
    )
        : base(compiler, name, type, llvmVariable)
    {
        Parent = parent;
        Attributes = attributes;
        Privacy = privacy;
    }
}
