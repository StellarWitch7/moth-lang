using Moth.AST.Node;

namespace Moth.LLVM.Data;

//TODO:
// this is the underlying type of #IInterface*
// it should not be possible to create an instance of this directly
public class TraitDecl : TypeDecl<TraitDecl>
{
    public VTableDef VTable { get; } = new VTableDef();
    public override bool IsUnion { get => false; }

    public TraitDecl(LLVMCompiler compiler, Namespace parent, string name, PrivacyType privacy, Dictionary<string, IAttribute> attributes)
        : base(compiler, parent, name, (llvmCompiler, decl) => LLVMTypeRef.Int8, privacy, attributes) { }

    public Function GetMethod(string name, IReadOnlyList<Type> paramTypes)
    {
        if (Methods.TryGetValue(name, out OverloadList overloads)
            && overloads.TryGet(paramTypes, out Function func))
        {
            return func;
        }
        else
        {
            throw new Exception($"Method \"{name}\" does not exist on type \"{Name}\".");
        }
    }
}
