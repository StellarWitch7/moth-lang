namespace Moth.AST.Node;

public class EnumFlagNode : IASTNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
    public string Name { get; set; }
    public ulong Value { get; set; }

    public EnumFlagNode(string name, ulong value)
    {
        Name = name;
        Value = value;
    }

    public string GetSource() => $"{Name} = {Value}";
}
