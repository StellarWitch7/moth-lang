using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Class : Struct
{
    public Dictionary<Signature, Function> Methods { get; } = new Dictionary<Signature, Function>();
    public Dictionary<string, Field> StaticFields { get; } = new Dictionary<string, Field>();
    public Dictionary<string, Constant> Constants { get; } = new Dictionary<string, Constant>();

    public Class(IContainer? parent, string name, LLVMTypeRef llvmType, PrivacyType privacy)
        : base(parent, name, llvmType, privacy) { }
}
