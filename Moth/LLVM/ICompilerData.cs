using Moth.AST;

namespace Moth.LLVM;

public interface ICompilerData
{
    bool IsExternal { get; init; }
    IASTNode? Node { get; init; }
}
