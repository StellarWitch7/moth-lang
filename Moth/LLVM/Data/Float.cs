using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Float : PrimitiveStructDecl
{
    public Float(LLVMCompiler compiler, string name, LLVMTypeRef llvmType, uint bitlength)
        : base(compiler, name, llvmType, bitlength) { }

    protected override Dictionary<string, OverloadList> GenerateDefaultMethods() =>
        throw new NotImplementedException();
}
