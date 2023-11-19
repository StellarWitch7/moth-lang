using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Namespace : CompilerData, INamespaceContainer, IFunctionContainer, IClassContainer
{
    public IContainer? Parent { get; }
    public string Name { get; }
    public Dictionary<string, Namespace> Namespaces { get; } = new Dictionary<string, Namespace>();
    public Dictionary<Signature, Function> Functions { get; } = new Dictionary<Signature, Function>();
    public Dictionary<Key, Class> Classes { get; } = new Dictionary<Key, Class>();
    public Dictionary<Key, Constant> Constants { get; } = new Dictionary<Key, Constant>();
    public Dictionary<Key, GenericClassNode> GenericClassTemplates { get; } = new Dictionary<Key, GenericClassNode>();
    public GenericDictionary GenericClasses { get; } = new GenericDictionary();

    public Namespace(IContainer? parent, string name)
    {
        Parent = parent;
        Name = name;
    }
    
    public Namespace GetNamespace(string name) => throw new NotImplementedException();

    public Function GetFunction(Signature sig) => throw new NotImplementedException();

    public Class GetClass(Key key) => throw new NotImplementedException();
}
