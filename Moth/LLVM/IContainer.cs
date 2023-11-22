using Moth.AST.Node;
using Moth.LLVM.Data;

namespace Moth.LLVM;

public interface IContainer
{
    public IContainer? Parent { get; }
}

public interface INamespaceContainer : IContainer
{
    public Namespace GetNamespace(string name);

    public bool TryGetNamespace(string name, out Namespace nmspace)
    {
        try
        {
            nmspace = GetNamespace(name);

            if (nmspace == null)
            {
                throw new Exception();
            }
            
            return true;
        }
        catch
        {
            nmspace = null;
            return false;
        }
    }
}

public interface IClassContainer : IContainer
{
    public Class GetClass(string key);

    public bool TryGetClass(string key, out Class @class)
    {
        try
        {
            @class = GetClass(key);

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

public interface IFieldContainer : IContainer
{
    public Field GetField(string key);
    
    public bool TryGetClass(string key, out Field field)
    {
        try
        {
            field = GetField(key);

            if (field == null)
            {
                throw new Exception();
            }
            
            return true;
        }
        catch
        {
            field = null;
            return false;
        }
    }
}