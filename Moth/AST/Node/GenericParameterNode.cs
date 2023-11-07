namespace Moth.AST.Node;

public class GenericParameterNode : ASTNode
{
    public string Name { get; set; }

    public GenericParameterNode(string name)
    {
        Name = name;
    }
}
