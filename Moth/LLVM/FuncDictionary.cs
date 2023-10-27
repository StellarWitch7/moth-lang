using Moth.LLVM.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class FuncDictionary : Dictionary<Signature, Function>
{
    public FuncDictionary() : base(new SigEqualityComparer())
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
            if (@param.Class.Name != y.Params[index].Class.Name
                || (@param is BasedType
                    && y.Params[index] is BasedType
                    && ((BasedType)@param).GetDepth() != ((BasedType)y.Params[index]).GetDepth())
                || (@param is RefType
                    && y.Params[index] is not RefType)
                || (@param is not RefType
                    && y.Params[index] is RefType))
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
