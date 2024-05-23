namespace Moth.AST.Node;

//TODO: this could just be a string
public class TemplateParameterNode : ASTNode
{
    public string Name { get; set; }

    public TemplateParameterNode(string name) => Name = name;
}
