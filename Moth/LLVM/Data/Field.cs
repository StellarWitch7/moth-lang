using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Field
{
    public string Name { get; }
    public uint FieldIndex { get; }
    public InternalType InternalType { get; }
    public PrivacyType Privacy { get; }

    public Field(string name, uint index, InternalType type, PrivacyType privacy)
    {
        Name = name;
        FieldIndex = index;
        InternalType = type;
        Privacy = privacy;
    }
}
