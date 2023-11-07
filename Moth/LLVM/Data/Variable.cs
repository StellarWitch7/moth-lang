using LLVMSharp.Interop;

namespace Moth.LLVM.Data;

public class Variable : CompilerData
{
    public string Name { get; set; }
    public LLVMValueRef LLVMVariable { get; set; }
    public Type Type { get; set; }

    public Variable(string name, LLVMValueRef llvmVariable, Type type)
    {
        Name = name;
        LLVMVariable = llvmVariable;
        Type = type;
    }
}
