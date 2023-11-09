using Moth.LLVM.Data;
using System.Diagnostics.CodeAnalysis;

namespace Moth.LLVM;

public class GenericDictionary : Dictionary<GenericSignature, Class>
{
    public GenericDictionary() : base(new GenericEqualityComparer())
    {
    }
}

internal class GenericEqualityComparer : IEqualityComparer<GenericSignature>
{
    public bool Equals(GenericSignature x, GenericSignature y) => throw new NotImplementedException();

    public int GetHashCode([DisallowNull] GenericSignature obj) => throw new NotImplementedException();
}
