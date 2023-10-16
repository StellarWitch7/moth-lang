﻿using Moth.LLVM.Data;
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
            return true;

        if (x.Name != y.Name)
        {
            return false;
        }

        bool isEqual = true;
        int index = 0;

        foreach (var @param in x.Params)
        {
            if (@param.IsPointer != y.Params[index].IsPointer
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
