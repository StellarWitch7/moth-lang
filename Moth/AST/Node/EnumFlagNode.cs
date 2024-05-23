namespace Moth.AST.Node;

public class EnumFlagNode : ASTNode
{
    public string Name { get; set; }

    public EnumFlagNode(string name)
    {
        Name = name;
    }

    public override string GetSource() => Name;
}
