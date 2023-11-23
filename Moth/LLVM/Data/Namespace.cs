using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Namespace : CompilerData, IContainer
{
    public IContainer? Parent { get; }
    public string Name { get; }
    public Dictionary<string, Namespace> Namespaces { get; } = new Dictionary<string, Namespace>();
    public Dictionary<Signature, Function> Functions { get; } = new Dictionary<Signature, Function>();
    public Dictionary<string, Struct> Structs { get; } = new Dictionary<string, Struct>();
    public Dictionary<string, Constant> Constants { get; } = new Dictionary<string, Constant>();
    public Dictionary<string, GenericClassNode> GenericClassTemplates { get; } = new Dictionary<string, GenericClassNode>();
    public GenericDictionary GenericClasses { get; } = new GenericDictionary();

    public Namespace(Namespace? parent, string name)
    {
        Parent = parent;
        Name = name;
    }
    
    public Namespace? ParentNamespace
    {
        get
        {
            return Parent is Namespace nmspace ? nmspace : null;
        }
    }

    public Namespace GetNamespace(string name)
    {
        if (Namespaces.TryGetValue(name, out Namespace nmspace))
        {
            return nmspace;
        }
        else
        {
            return ParentNamespace != null
                ? ParentNamespace.GetNamespace(name)
                : throw new Exception($"Namespace \"{name}\" was not found.");
        }
    }

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

    public Function GetFunction(Signature sig)
    {
        if (Functions.TryGetValue(sig, out Function func))
        {
            return func;
        }
        else
        {
            return ParentNamespace != null
                ? ParentNamespace.GetFunction(sig)
                : throw new Exception($"Function \"{sig}\" was not found.");
        }
    }

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

    public Struct GetStruct(string name)
    {
        if (Structs.TryGetValue(name, out Struct @struct))
        {
            return @struct;
        }
        else
        {
            return ParentNamespace != null
                ? ParentNamespace.GetStruct(name)
                : throw new Exception($"Type \"{name}\" was not found.");
        }
    }
    
    public bool TryGetStruct(string name, out Struct @struct)
    {
        try
        {
            @struct = GetStruct(name);

            if (@struct == null)
            {
                throw new Exception();
            }
            
            return true;
        }
        catch
        {
            @struct = null;
            return false;
        }
    }
}
