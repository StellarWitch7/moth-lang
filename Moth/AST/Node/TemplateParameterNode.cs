namespace Moth.AST.Node;

//TODO: this could just be a string
public class TemplateParameterNode : IASTNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
    public string Name { get; set; }

    public TemplateParameterNode(string name) => Name = name;

    public string GetSource()
    {
        return Name;
    }
}
