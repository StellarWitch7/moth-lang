using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Class : Struct
{
    public Dictionary<Signature, Function> Methods { get; } = new Dictionary<Signature, Function>();
    public Dictionary<string, Constant> Constants { get; } = new Dictionary<string, Constant>();

    public Class(Namespace? parent, string name, LLVMTypeRef llvmType, PrivacyType privacy)
        : base(parent, name, llvmType, privacy) { }

    public Function GetMethod(Signature sig, Struct? currentStruct)
    {
        if (Methods.TryGetValue(sig, out Function func))
        {
            if (func is DefinedFunction defFunc && defFunc.Privacy == PrivacyType.Private && currentStruct != this)
            {
                throw new Exception($"Cannot access private method \"{sig}\" on type \"{Name}\".");
            }

            return func;
        }
        else
        {
            throw new Exception($"Method \"{sig}\" does not exist on type \"{Name}\".");
        }
    }

    public override Variable Init(LLVMCompiler compiler)
    {
        var @new = new Variable(Reserved.Self,
            compiler.WrapAsRef(this),
            compiler.Builder.BuildAlloca(LLVMTypeRef.CreatePointer(LLVMType, 0)));
        @new.Store(compiler, new Value(this, compiler.Builder.BuildMalloc(this.LLVMType)));
        return @new;
    }
}
