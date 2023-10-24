using Moth.LLVM.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class SignedDictionary : Dictionary<Signature, Function>
{
    public SignedDictionary() : base(new SigEqualityComparer())
    {
    }
}

class SigEqualityComparer : IEqualityComparer<Signature>
{
    public bool Equals(Signature x, Signature y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x.Name != y.Name)
        {
            return false;
        }

        if (!(x.IsVariadic || y.IsVariadic))
        {
            if (x.Params.Length != y.Params.Length)
            {
                return false;
            }
        }

        bool isEqual = true;
        int index = 0;

        foreach (var @param in x.Params.Length < y.Params.Length ? x.Params : y.Params)
        {
            if (@param.PointerDepth != y.Params[index].PointerDepth
                || @param.Name != y.Params[index].Name)
            {
                isEqual = false;
                break;
            }

            index++;
        }

        return isEqual;
    }

    public int GetHashCode([DisallowNull] Signature obj) => 3 * obj.Name.GetHashCode() * obj.Name.GetTypeCode().GetHashCode();
}
