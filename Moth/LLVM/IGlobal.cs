using Moth.AST.Node;
using Moth.LLVM.Data;

namespace Moth.LLVM;

public interface IGlobal
{
    public PrivacyType Privacy { get; }
    public string Name { get; }
    public VarType Type { get; }
    public LLVMValueRef LLVMValue { get; }

    public string GetInvalidTypeErrorMsg(Value value);

    public void SetInitializer(LLVMCompiler compiler, Value value)
    {
        if (!Type.BaseType.Equals(value.Type))
        {
            throw new Exception(GetInvalidTypeErrorMsg(value));
        }

        var val = LLVMValue;
        val.Initializer = value.LLVMValue;
    }
}
