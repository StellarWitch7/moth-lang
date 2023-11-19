using Moth.AST.Node;

namespace Moth.LLVM;

public class Key
{
    public string Name { get; }
    public PrivacyType Privacy { get; }

    public Key(string name, PrivacyType privacy)
    {
        Name = name;
        Privacy = privacy;
    }
    
    public override string ToString() => Privacy switch
    {
        PrivacyType.Local => Reserved.Local,
        PrivacyType.Private => Reserved.Private,
        PrivacyType.Public => Reserved.Public,
    } + " " + Name;

    public override bool Equals(object? obj)
    {
        if (obj is not Key key)
        {
            return false;
        }

        if (ReferenceEquals(this, key))
        {
            return true;
        }

        if (Name != key.Name)
        {
            return false;
        }
        
        
    }

    public override int GetHashCode() => Name.GetHashCode();
}
