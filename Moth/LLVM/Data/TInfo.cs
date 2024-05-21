namespace Moth.LLVM.Data;

public class TInfo //TODO: compiler data?
{
    public LLVMValueRef LLVMValue { get; }
    public TInfo(LLVMCompiler compiler, Type type)
    {
        var llvmType = LLVMTypeRef.CreateInt(128);
        var global = compiler.Module.AddGlobal(llvmType, $"<TInfo/{type.FullName}>");
        
        global.Initializer = LLVMValueRef.CreateConstIntOfArbitraryPrecision(llvmType, type.UUID.ToByteArray().ToULong());
        global.Linkage = LLVMLinkage.LLVMDLLExportLinkage;
        global.IsGlobalConstant = true;

        LLVMValue = global;
    }
}
