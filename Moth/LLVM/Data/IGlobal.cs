using Moth.AST.Node;
using Moth.LLVM.Data;

namespace Moth.LLVM.Data;

public interface IGlobal : ICompilerData
{
    public Namespace Parent { get; }
    public Dictionary<string, IAttribute> Attributes { get; }
    public PrivacyType Privacy { get; }
    public string Name { get; }
    public VarType InternalType { get; }
    public LLVMValueRef LLVMValue { get; }

    public string GetInvalidTypeErrorMsg(Value value);

    public void SetInitializer(LLVMCompiler compiler, Value value)
    {
        if (!InternalType.BaseType.Equals(value.InternalType))
        {
            throw new Exception(GetInvalidTypeErrorMsg(value));
        }

        var val = LLVMValue;
        val.Initializer = value.LLVMValue;
    }

    public string FullName
    {
        get
        {
            return $"{Parent.FullName}.{Name}";
        }
    }
}
