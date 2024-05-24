using Moth.LLVM.Data;
using Type = Moth.LLVM.Data.Type;

namespace Moth.LLVM;

public interface IContainer : ICompilerData
{
    public IContainer? Parent { get; }
    public string FullName { get; }

    public Function GetFunction(string name, IReadOnlyList<Type> paramTypes, TypeDecl? currentTypeDecl, bool recursive);

    public bool TryGetFunction(string name, IReadOnlyList<Type> paramTypes, TypeDecl? currentTypeDecl, bool recursive, out Function func);
}
