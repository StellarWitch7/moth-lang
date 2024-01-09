using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Class : Struct
{

    public Class(Namespace? parent, string name, LLVMTypeRef llvmType, PrivacyType privacy)
        : base(parent, name, llvmType, privacy) { }

    public override Variable Init(LLVMCompiler compiler)
    {
        var @new = new Variable(Reserved.Self,
            compiler.WrapAsRef(new PtrType(this)),
            compiler.Builder.BuildAlloca(LLVMTypeRef.CreatePointer(LLVMType, 0)));
        @new.Store(compiler, Value.Create(new PtrType(this), compiler.Builder.BuildMalloc(this.LLVMType)));
        return @new;
    }
}
