using System.Runtime.InteropServices;
using Type = Moth.LLVM.Type;
using Value = Moth.LLVM.Data.Value;

namespace Moth;

public static class ListExtensions
{
    public static ReadOnlySpan<T> AsReadonlySpan<T>(this List<T> list)
    {
        Span<T> span = CollectionsMarshal.AsSpan(list);
        return span[..list.Count];
    }

    public static List<LLVMTypeRef> AsLLVMTypes(this List<Type> types)
    {
        var result = new List<LLVMTypeRef>();

        foreach (Type type in types)
        {
            result.Add(type.LLVMType);
        }

        return result;
    }
}

public static class ArrayExtensions
{
    public static LLVMValueRef[] AsLLVMValues(this Value[] values)
    {
        var result = new LLVMValueRef[values.Length];
        uint index = 0;

        foreach (Value value in values)
        {
            result[index] = value.LLVMValue;
            index++;
        }

        return result;
    }

    public static LLVMTypeRef[] AsLLVMTypes(this Type[] types)
    {
        var result = new LLVMTypeRef[types.Length];
        uint index = 0;

        foreach (Type type in types)
        {
            result[index] = type.LLVMType;
            index++;
        }

        return result;
    }

    public static int GetHashes(this Type[] types)
    {
        int hash = 3;

        foreach (Type type in types)
        {
            hash *= 31 + type.GetHashCode();
        }

        return hash;
    }
}