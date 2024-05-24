using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Float : PrimitiveStructDecl
{
    public Float(string name, LLVMTypeRef llvmType, uint bitlength) : base(name, llvmType, bitlength) { }

    protected override Dictionary<string, OverloadList> GenerateDefaultMethods() => throw new NotImplementedException();
}
