using Moth.LLVM.Data;

namespace Moth.LLVM;

public interface IContainer
{
    public IContainer? Parent { get; }
    public CompilerData GetData(string name);

    public bool TryGetData(string name, out CompilerData data)
    {
        try
        {
            data = GetData(name);

            if (data == null)
            {
                throw new Exception();
            }
            
            return true;
        }
        catch
        {
            data = null;
            return false;
        }
    }
}

public interface IFunctionContainer : IContainer
{
    public Function GetFunction(Signature sig);

    public bool TryGetFunction(Signature sig, out Function func)
    {
        try
        {
            func = GetFunction(sig);

            if (func == null)
            {
                throw new Exception();
            }
            
            return true;
        }
        catch
        {
            func = null;
            return false;
        }
    }
}

public interface IClassContainer : IContainer
{
    public Class GetClass(string name);

    public bool TryGetClass(string name, out Class @class)
    {
        try
        {
            @class = GetClass(name);

            if (@class == null)
            {
                throw new Exception();
            }
            
            return true;
        }
        catch
        {
            @class = null;
            return false;
        }
    }
}