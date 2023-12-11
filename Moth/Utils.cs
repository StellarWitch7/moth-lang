using Moth.AST.Node;
using Moth.LLVM;
using Moth.LLVM.Data;
using System.Runtime.InteropServices;

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
    public static LLVMValueRef[] AsLLVMValues(this byte[] bytes)
    {
        var result = new LLVMValueRef[bytes.Length];
        uint index = 0;

        foreach (var @byte in bytes)
        {
            result[index] = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, @byte);
            index++;
        }

        return result;
    }
    
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

    public static bool TryGetNamespace(this Namespace[] imports, string name, out Namespace nmspace)
    {
        nmspace = null;
        
        foreach (var import in imports)
        {
            if (import.Namespaces.TryGetValue(name, out nmspace))
            {
                break;
            }
            else if (import.Name == name)
            {
                nmspace = import;
                break;
            }
        }

        if (nmspace != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool TryGetFunction(this Namespace[] imports, Signature sig, out Function func)
    {
        func = null;
        
        foreach (var import in imports)
        {
            if (import.Functions.TryGetValue(sig, out func))
            {
                if (func is DefinedFunction defFunc && defFunc.Privacy == PrivacyType.Private)
                {
                    func = null;
                }
                else
                {
                    break;
                }
            }
        }

        if (func != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool TryGetStruct(this Namespace[] imports, string name, out Struct @struct)
    {
        @struct = null;
        
        foreach (var import in imports)
        {
            if (import.Structs.TryGetValue(name, out @struct))
            {
                if (@struct.Privacy == PrivacyType.Private)
                {
                    @struct = null;
                }
                else
                {
                    break;
                }
            }
        }

        if (@struct != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}