namespace Moth.LLVM.Data;

public class ConstVar : Variable
{
    private bool _hasNoValue = true;
    
    public ConstVar(string name, RefType type, LLVMValueRef llvmVariable) : base(name, type, llvmVariable) { }

    public override Pointer Store(LLVMCompiler compiler, Value value)
    {
        if (_hasNoValue)
        {
            return base.Store(compiler, value);
            _hasNoValue = false;
        }
        else
        {
            throw new Exception($"Constant \"{Name}\" cannot be reassigned!");
        }
    }
}
