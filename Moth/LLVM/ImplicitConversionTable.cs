using System.Diagnostics.CodeAnalysis;

namespace Moth.LLVM;

public class ImplicitConversionTable
{
    protected LLVMCompiler _compiler;
    private Dictionary<Data.Type, Func<Value, Value>> _converters =
        new Dictionary<Data.Type, Func<Value, Value>>();

    public ImplicitConversionTable(LLVMCompiler compiler) => _compiler = compiler;

    public virtual bool Contains(Data.Type key)
    {
        return _converters.ContainsKey(key);
    }

    public virtual bool TryGetValue(
        Data.Type key,
        [MaybeNullWhen(false)] out Func<Value, Value> value
    )
    {
        return _converters.TryGetValue(key, out value);
    }

    public virtual void Add(Data.Type key, Func<Value, Value> value)
    {
        _converters.Add(key, value);
    }

    public virtual void Remove(Data.Type key)
    {
        _converters.Remove(key);
    }
}
