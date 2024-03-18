namespace Moth.AST.Node;

public class TemplateParameterNode : ASTNode
{
    public string Name { get; set; }

    public TemplateParameterNode(string name) => Name = name;
}
