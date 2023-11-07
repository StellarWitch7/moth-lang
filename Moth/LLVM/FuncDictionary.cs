using Moth.LLVM.Data;

namespace Moth.LLVM;

public class FuncDictionary : Dictionary<Signature, Function>
{
    public FuncDictionary() : base()
    {
    }
}