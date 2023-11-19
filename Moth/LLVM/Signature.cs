namespace Moth.LLVM;

public class Signature
{
    public Key Key { get; }
    public bool IsVariadic { get; set; }
    public IReadOnlyList<Type> Params { get; set; }

    public Signature(Key key, IReadOnlyList<Type> @params, bool isVariadic = false)
    {
        Key = key;
        Params = @params;
        IsVariadic = isVariadic;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append($"{Key}:");

        foreach (Type param in Params)
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

        if (!Key.Equals(sig.Key))
        {
            return false;
        }

        if (!(IsVariadic || sig.IsVariadic))
        {
            if (ToString() != sig.ToString())
            {
                return false;
            }
        }

        bool isEqual = true;
        int index = 0;

        foreach (Type @param in Params.Count < sig.Params.Count ? Params : sig.Params)
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

    public override int GetHashCode() => 3 * Key.GetHashCode();
}
