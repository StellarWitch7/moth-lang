using Moth.LLVM.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
