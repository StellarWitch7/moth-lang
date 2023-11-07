using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Field : CompilerData
{
    public string Name { get; set; }
    public uint FieldIndex { get; set; }
    public Type Type { get; set; }
    public PrivacyType Privacy { get; set; }

    public Field(string name, uint index, Type type, PrivacyType privacy)
    {
        Name = name;
        FieldIndex = index;
        Type = type;
        Privacy = privacy;
    }
}
