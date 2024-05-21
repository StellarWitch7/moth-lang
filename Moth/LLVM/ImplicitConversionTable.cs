using System.Diagnostics.CodeAnalysis;

namespace Moth.LLVM;

public class ImplicitConversionTable
{
    private Dictionary<InternalType, Func<LLVMCompiler, Value, Value>> _converters = new Dictionary<InternalType, Func<LLVMCompiler, Value, Value>>();
    
    public virtual bool Contains(InternalType key)
    {
        return _converters.ContainsKey(key);
    }

    public virtual bool TryGetValue(InternalType key, [MaybeNullWhen(false)] out Func<LLVMCompiler, Value, Value> value)
    {
        return _converters.TryGetValue(key, out value);
    }

    public virtual void Add(InternalType key, Func<LLVMCompiler, Value, Value> value)
    {
        _converters.Add(key, value);
    }

    public virtual void Remove(InternalType key)
    {
        _converters.Remove(key);
    }
}