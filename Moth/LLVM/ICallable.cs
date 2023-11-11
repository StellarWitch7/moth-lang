using Moth.LLVM.Data;

namespace Moth.LLVM;

public interface ICallable
{
    public Type ReturnType { get; }
    public Type[] ParameterTypes { get; }

    public Value Call(LLVMCompiler compiler, Function func, Value[] args);
}