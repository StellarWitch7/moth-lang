using Moth.AST.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class Signature
{
    public string Name { get; set; }
    public Type[] Params { get; set; }
    public bool IsVariadic { get; set; }

    public Signature(string name, Type[] @params, bool isVariadic = false)
    {
        Name = name;
        Params = @params;
        IsVariadic = isVariadic;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append($"{Name}:");

        foreach (var param in Params)
        {
            builder.Append($"{param.ToString()}.");
        }

        builder.Remove(builder.Length - 1, 1);
        return builder.ToString();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Signature sig)
        {
            return false;
        }

        if (ReferenceEquals(this, sig))
        {
            return true;
        }

        if (Name != sig.Name)
        {
            return false;
        }

        if (!(IsVariadic || sig.IsVariadic))
        {
            if (Params.Length != sig.Params.Length)
            {
                return false;
            }
        }

        bool isEqual = true;
        int index = 0;

        foreach (var @param in Params.Length < sig.Params.Length ? Params : sig.Params)
        {
            if (!@param.Equals(sig.Params[index]))
            {
                isEqual = false;
                break;
            }

            index++;
        }

        return isEqual;
    }

    public override int GetHashCode() => 3 * Name.GetHashCode();
}
