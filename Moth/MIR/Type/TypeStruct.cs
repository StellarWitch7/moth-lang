namespace Moth.MIR.Type;

public class TypeStruct
{
    public string Name { get; }
    public MIRType[] Fields { get; }

    public TypeStruct(MIRType[] fields, string name = null)
    {
        Name = name;
        Fields = fields;
    }

    public override string ToString()
    {
        return Name == null
            ? $"{{ {String.Join(", ", Fields.ExecuteOverAll(v => $"{v}"))} }}"
            : $"#\"{Name}\"";
    }
}
