using System.Runtime.InteropServices;
using Type = Moth.LLVM.Type;
using Value = Moth.LLVM.Value;

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
}