namespace Moth.AST.Node;

public class ImplementNode : IDeclNode, ITopDeclNode
{
    public ITypeRefNode Type { get; set; }
    public ITypeRefNode Trait { get; set; }
    public ScopeNode Implementations { get; set; }

    public ImplementNode(ITypeRefNode type, ITypeRefNode trait, ScopeNode implementations)
    {
        Type = type;
        Trait = trait;
        Implementations = implementations;
    }

    public string GetSource()
    {
        throw new NotImplementedException();
    }
}
