namespace Moth.LLVM.Data;

public class TInfo //TODO: compiler data?
{
    public LLVMValueRef LLVMValue { get; }

    public TInfo(LLVMCompiler compiler, TypeDecl typeDecl)
    {
        var llvmType = LLVMTypeRef.CreateInt(128);
        var global = compiler.Module.AddGlobal(llvmType, $"<TInfo/{typeDecl.FullName}>");

        global.Initializer = LLVMValueRef.CreateConstIntOfArbitraryPrecision(
            llvmType,
            typeDecl.UUID.ToByteArray().ToULong()
        );
        global.Linkage = LLVMLinkage.LLVMDLLExportLinkage;
        global.IsGlobalConstant = true;

        LLVMValue = global;
    }
}
