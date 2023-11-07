using System.Diagnostics.CodeAnalysis;
using Moth.LLVM.Data;

namespace Moth.LLVM;

public class GenericDictionary : Dictionary<GenericSignature, Class>
{
    public GenericDictionary() : base(new GenericEqualityComparer())
    {
    }
}

class GenericEqualityComparer : IEqualityComparer<GenericSignature>
{
    public bool Equals(GenericSignature x, GenericSignature y)
    {
        throw new NotImplementedException();
    }

    public int GetHashCode([DisallowNull] GenericSignature obj) => throw new NotImplementedException();
}
