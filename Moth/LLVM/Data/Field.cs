using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Field : CompilerData
{
    public string Name { get; }
    public uint FieldIndex { get; }
    public Type Type { get; }
    public PrivacyType Privacy { get; }

    public Field(string name, uint index, Type type, PrivacyType privacy)
    {
        Name = name;
        FieldIndex = index;
        Type = type;
        Privacy = privacy;
    }
}
