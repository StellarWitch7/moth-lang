using Moth.AST.Node;
using Moth.LLVM;
using Moth.LLVM.Data;
using System.Runtime.InteropServices;

namespace Moth;

public static class Utils
{
    public static LLVMCallConv StringToCallConv(string str)
    {
        return str switch
        {
            "cdecl" => LLVMCallConv.LLVMCCallConv,
            _ => throw new Exception($"Invalid calling convention: \"{str}\".")
        };
    }
    
    public static OS StringToOS(string str)
    {
        return str switch
        {
            "windows" => OS.Windows,
            "linux" => OS.Linux,
            "macos" => OS.MacOS,
            _ => throw new Exception($"Invalid OS: \"{str}\".")
        };
    }

    public static OS GetOS()
    {
        if (OperatingSystem.IsWindows()) return OS.Windows;
        if (OperatingSystem.IsLinux()) return OS.Linux;
        if (OperatingSystem.IsMacOS()) return OS.MacOS;
        throw new PlatformNotSupportedException();
    }

    public static bool IsOS(OS os)
    {
        return os switch
        {
            OS.Windows => OperatingSystem.IsWindows(),
            OS.Linux => OperatingSystem.IsLinux(),
            OS.MacOS => OperatingSystem.IsMacOS(),
            _ => throw new Exception($"Cannot verify that the current OS is \"{os}\".")
        };
    }
    
    public static byte[] Unescape(string original)
    {
        if (original.Length == 0)
        {
            return new byte[0];
        }

        uint index = 0;
        var bytes = new List<byte>();

        while (index < original.Length)
        {
            var ch = original[(int)index];

            if (ch == '\\')
            {
                index++;
                var hex1 = original[(int)index];
                index++;
                var hex2 = original[(int)index];

                var byte1 = hex1 switch
                {
                    >= '0' and <= '9' => (byte) hex1 - (byte) '0',
                    >= 'a' and <= 'f' => (byte) hex1 - (byte) 'a' + 10,
                    >= 'A' and <= 'F' => (byte) hex1 - (byte) 'A' + 10,
                    _ => throw new ArgumentOutOfRangeException(nameof(hex1)),
                };
            
                var byte2 = hex2 switch
                {
                    >= '0' and <= '9' => (byte) hex2 - (byte) '0',
                    >= 'a' and <= 'f' => (byte) hex2 - (byte) 'a' + 10,
                    >= 'A' and <= 'F' => (byte) hex2 - (byte) 'A' + 10,
                    _ => throw new ArgumentOutOfRangeException(nameof(hex2)),
                };

                bytes.Add((byte) ((byte1 << 4) | byte2));
                index++;
            }
            else
            {
                bytes.Add((byte) ch);
                index++;
            }
        }

        return bytes.ToArray();
    }
}

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
    public static RESULT[] ExecuteOverAll<ORIGINAL, RESULT>(this ORIGINAL[] original, Func<ORIGINAL, RESULT> func)
    {
        var result = new List<RESULT>();

        foreach (var val in original)
        {
            result.Add(func(val));
        }

        return result.ToArray();
    }

    public static Value[] CompileToValues(this ExpressionNode[] expressionNodes, LLVMCompiler compiler, Scope scope)
    {
        var result = new List<Value>();

        foreach (var expr in expressionNodes)
        {
            result.Add(compiler.CompileExpression(scope, expr));
        }

        return result.ToArray();
    }
    
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

    public static Value[] SafeLoadAll(this Value[] values, LLVMCompiler compiler)
    {
        var result = new Value[values.Length];
        uint index = 0;

        foreach (Value value in values)
        {
            result[index] = compiler.SafeLoad(value);
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

    public static LLVMTypeRef[] AsLLVMTypes(this Field[] fields)
    {
        var types = new List<Type>();

        foreach (var field in fields)
        {
            types.Add(field.Type);
        }

        return types.ToArray().AsLLVMTypes();
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