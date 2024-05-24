namespace Moth.LLVM.Data;

public class ConstVar : Variable
{
    private bool _hasNoValue = true;

    public ConstVar(LLVMCompiler compiler, string name, VarType type, LLVMValueRef llvmVariable)
        : base(compiler, name, type, llvmVariable) { }

    public override Pointer Store(Value value)
    {
        if (_hasNoValue)
        {
            return base.Store(value);
            _hasNoValue = false;
        }
        else
        {
            throw new Exception($"Constant \"{Name}\" cannot be reassigned!");
        }
    }
}
