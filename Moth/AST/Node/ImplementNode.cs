namespace Moth.AST.Node;

public class ImplementNode : ASTNode
{
    public TypeRefNode Type { get; set; }
    public TypeRefNode Trait { get; set; }
    public ScopeNode Implementations { get; set; }

    public ImplementNode(TypeRefNode type, TypeRefNode trait, ScopeNode implementations)
    {
        Type = type;
        Trait = trait;
        Implementations = implementations;
    }
}
