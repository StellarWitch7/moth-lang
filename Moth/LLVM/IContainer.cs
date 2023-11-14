using Moth.LLVM.Data;

namespace Moth.LLVM;

public interface IContainer
{
    public IContainer? Parent { get; }
    public CompilerData GetData(string name);
    public bool TryGetData(string name, out CompilerData data);
}

public interface IFunctionContainer : IContainer
{
    public FuncVal GetFunction(Signature sig);
    public bool TryGetFunction(Signature sig, out FuncVal func);
}

public interface IClassContainer : IContainer
{
    public Class GetClass(string name);
    public bool TryGetClass(string name, out Class @class);
}