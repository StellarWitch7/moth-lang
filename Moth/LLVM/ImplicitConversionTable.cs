using System.Diagnostics.CodeAnalysis;
using Type = Moth.LLVM.Data.Type;

namespace Moth.LLVM;

public class ImplicitConversionTable
{
    protected LLVMCompiler _compiler;
    private Dictionary<Type, Func<Value, Value>> _converters =
        new Dictionary<Type, Func<Value, Value>>();

    public ImplicitConversionTable(LLVMCompiler compiler) => _compiler = compiler;

    public virtual bool Contains(Type key)
    {
        return _converters.ContainsKey(key);
    }

    public virtual bool TryGetValue(Type key, [MaybeNullWhen(false)] out Func<Value, Value> value)
    {
        return _converters.TryGetValue(key, out value);
    }

    public virtual void Add(Type key, Func<Value, Value> value)
    {
        _converters.Add(key, value);
    }

    public virtual void Remove(Type key)
    {
        _converters.Remove(key);
    }
}
