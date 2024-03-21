using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Namespace : CompilerData, IContainer
{
    public IContainer? Parent { get; }
    public string Name { get; }
    public Dictionary<string, Namespace> Namespaces { get; } = new Dictionary<string, Namespace>();
    public Dictionary<string, OverloadList> Functions { get; } = new Dictionary<string, OverloadList>();
    public Dictionary<string, Struct> Structs { get; } = new Dictionary<string, Struct>();
    public Dictionary<string, IGlobal> GlobalVariables { get; } = new Dictionary<string, IGlobal>();
    public Dictionary<string, Template> Templates { get; } = new Dictionary<string, Template>();

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

    public string FullName
    {
        get
        {
            if (ParentNamespace == null)
            {
                return Name;
            }
            else
            {
                return $"{ParentNamespace.FullName}::{Name}";
            }
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

    public Function GetFunction(string name, IReadOnlyList<Type> paramTypes)
    {
        if (Functions.TryGetValue(name, out OverloadList overloads)
            && overloads.TryGet(paramTypes, out Function func))
        {
            return func;
        }
        else
        {
            return ParentNamespace != null
                ? ParentNamespace.GetFunction(name, paramTypes)
                : throw new Exception($"Function \"{name}\" was not found.");
        }
    }

    public bool TryGetFunction(string name, IReadOnlyList<Type> paramTypes, out Function func)
    {
        try
        {
            func = GetFunction(name, paramTypes);

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
                : throw new Exception($"Type \"{name}\" was not found in namespace \"{Name}\"");
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
    
    public Template GetTemplate(string name)
    {
        if (Templates.TryGetValue(name, out Template template))
        {
            return template;
        }
        else
        {
            return ParentNamespace != null
                ? ParentNamespace.GetTemplate(name)
                : throw new Exception($"Template \"{name}\" was not found in namespace \"{Name}\"");
        }
    }
    
    public bool TryGetTemplate(string name, out Template template)
    {
        try
        {
            template = GetTemplate(name);

            if (template == null)
            {
                throw new Exception();
            }
            
            return true;
        }
        catch
        {
            template = null;
            return false;
        }
    }
    
    public IGlobal GetGlobal(string name)
    {
        if (GlobalVariables.TryGetValue(name, out IGlobal global))
        {
            return global;
        }
        else
        {
            return ParentNamespace != null
                ? ParentNamespace.GetGlobal(name)
                : throw new Exception($"Global variable \"{name}\" was not found in namespace \"{Name}\"");
        }
    }
    
    public bool TryGetGlobal(string name, out IGlobal globalVar)
    {
        try
        {
            globalVar = GetGlobal(name);

            if (globalVar == null)
            {
                throw new Exception();
            }
            
            return true;
        }
        catch
        {
            globalVar = null;
            return false;
        }
    }
}
