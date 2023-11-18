using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Namespace : CompilerData, IClassContainer, IFunctionContainer
{
    public IContainer? Parent { get; }
    public string Name { get; }
    public Dictionary<string, Namespace> Namespaces { get; } = new Dictionary<string, Namespace>();
    public Dictionary<Signature, Function> Functions { get; } = new Dictionary<Signature, Function>();
    public Dictionary<string, Class> Classes { get; } = new Dictionary<string, Class>();
    public Dictionary<string, Constant> Constants { get; } = new Dictionary<string, Constant>();
    public Dictionary<string, GenericClassNode> GenericClassTemplates { get; } = new Dictionary<string, GenericClassNode>();
    public GenericDictionary GenericClasses { get; } = new GenericDictionary();

    public Namespace(IContainer? parent, string name)
    {
        Parent = parent;
        Name = name;
    }
    
    public CompilerData GetData(string name) => throw new NotImplementedException();

    public Function GetFunction(Signature sig) => throw new NotImplementedException();

    public Class GetClass(string name) => throw new NotImplementedException();
}
