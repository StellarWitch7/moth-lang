namespace Moth.AST.Node;

public class EnumFlagNode : IASTNode
{
    public string Name { get; set; }
    public ulong Value { get; set; }

    public EnumFlagNode(string name, ulong value)
    {
        Name = name;
        Value = value;
    }

    public string GetSource() => $"{Name} = {Value}";
}
