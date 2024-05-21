using Moth.AST.Node;

namespace Moth.LLVM.Data;

//TODO:
// this is the underlying type of #IInterface*
// it should not be possible to create an instance of this directly
public class Trait : InternalType, IContainer
{
    public IContainer? Parent { get; }
    public string Name { get; }
    public Dictionary<string, IAttribute> Attributes { get; }
    public PrivacyType Privacy { get; }
    public Dictionary<string, OverloadList> Methods { get; } = new Dictionary<string, OverloadList>();
    // public Dictionary<string, Property> Properties { get; } = new Dictionary<string, Property>();
    public VTableDef VTable { get; } = new VTableDef();
    
    public Trait(Namespace? parent, string name, Dictionary<string, IAttribute> attributes, PrivacyType privacy)
        : base(LLVMTypeRef.Void, TypeKind.Struct)
    {
        Parent = parent;
        Name = name;
        Attributes = attributes;
        Privacy = privacy;
    }

    public string FullName
    {
        get
        {
            return $"{Parent.FullName}#{Name}";
        }
    }

    public Function GetMethod(string name, IReadOnlyList<InternalType> paramTypes)
    {
        if (Methods.TryGetValue(name, out OverloadList overloads)
            && overloads.TryGet(paramTypes, out Function func))
        {
            return func;
        }
        else
        {
            throw new Exception($"Method \"{name}\" does not exist on type \"{Name}\".");
        }
    }
}
