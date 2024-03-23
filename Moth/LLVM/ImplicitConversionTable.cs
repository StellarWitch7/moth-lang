using System.Diagnostics.CodeAnalysis;

namespace Moth.LLVM;

public class ImplicitConversionTable
{
    private Dictionary<Type, Func<LLVMCompiler, Value, Value>> _converters = new Dictionary<Type, Func<LLVMCompiler, Value, Value>>();
    
    public virtual bool Contains(Type key)
    {
        return _converters.ContainsKey(key);
    }

    public virtual bool TryGetValue(Type key, [MaybeNullWhen(false)] out Func<LLVMCompiler, Value, Value> value)
    {
        return _converters.TryGetValue(key, out value);
    }

    public virtual void Add(Type key, Func<LLVMCompiler, Value, Value> value)
    {
        _converters.Add(key, value);
    }

    public virtual void Remove(Type key)
    {
        _converters.Remove(key);
    }
}