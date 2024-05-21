namespace Moth.LLVM;

public interface IContainer : ICompilerData
{
    public IContainer? Parent { get; }
    public string FullName { get; }
}
