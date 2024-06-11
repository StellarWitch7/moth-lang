using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Moth.AST.Node;
using Moth.LLVM;
using Moth.LLVM.Data;

namespace Moth;

public static class Utils
{
    public static void TypeAutoExport(LLVMCompiler compiler, Type type, bool child = false)
    {
        if (type is StructDecl structDecl)
        {
            if (structDecl is PrimitiveStructDecl)
                return;

            if (child && !compiler.Header.Structs.Contains(structDecl))
                structDecl = new OpaqueStructDecl(
                    compiler,
                    structDecl.ParentNamespace,
                    structDecl.Name,
                    structDecl.Privacy,
                    structDecl.IsUnion,
                    structDecl.Attributes
                );

            compiler.Header.Structs.Add(structDecl);
        }
        else if (type is PtrType ptrType)
        {
            TypeAutoExport(compiler, ptrType.BaseType, true);
        }
    }

    public static string MakeStubName(Language lang, string orig)
    {
        string result;

        if (lang == Language.C)
        {
            int i = orig.IndexOf('(');

            if (i != -1)
                result = orig.Remove(i);
            else
                result = orig;

            result = result
                .Replace("root::", "")
                .Replace("root#", "")
                .Replace("root.", "")
                .Replace("::", "_")
                .Replace("#", "_")
                .Replace(".", "_");
        }
        else
        {
            throw new NotImplementedException();
        }

        return result;
    }

    public static string ExpandOpName(string op)
    {
        return $"{Reserved.Operator}{{{op}}}";
    }

    public static string OpTypeToString(OperationType opType)
    {
        return opType switch
        {
            OperationType.Addition => "+",
            OperationType.Subtraction => "-",
            OperationType.Multiplication => "*",
            OperationType.Division => "/",
            OperationType.Exponential => "^",
            OperationType.Modulus => "%",
            OperationType.LesserThan => "<",
            OperationType.GreaterThan => ">",
            OperationType.LesserThanOrEqual => "<=",
            OperationType.GreaterThanOrEqual => ">=",
            OperationType.Equal => "==",
            //OperationType.Range => "..",

            _ => throw new NotImplementedException($"Unsupported operation type: \"{opType}\"")
        };
    }

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
            Reserved.Windows => OS.Windows,
            Reserved.Linux => OS.Linux,
            Reserved.MacOS => OS.MacOS,
            _ => throw new Exception($"Invalid OS: \"{str}\".")
        };
    }

    public static Language StringToLanguage(string str)
    {
        return str switch
        {
            Reserved.C => Language.C,
            Reserved.CPP => Language.CPP,
            _ => throw new Exception($"Invalid language: \"{str}\".")
        };
    }

    public static OS GetOS()
    {
        if (OperatingSystem.IsWindows())
            return OS.Windows;
        if (OperatingSystem.IsLinux())
            return OS.Linux;
        if (OperatingSystem.IsMacOS())
            return OS.MacOS;
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

                if (hex1 == '\\')
                {
                    bytes.Add((byte)hex1);
                    index++;
                }
                else
                {
                    index++;
                    var hex2 = original[(int)index];

                    var byte1 = hex1 switch
                    {
                        >= '0' and <= '9' => (byte)hex1 - (byte)'0',
                        >= 'a' and <= 'f' => (byte)hex1 - (byte)'a' + 10,
                        >= 'A' and <= 'F' => (byte)hex1 - (byte)'A' + 10,
                        _ => throw new ArgumentOutOfRangeException(nameof(hex1)),
                    };

                    var byte2 = hex2 switch
                    {
                        >= '0' and <= '9' => (byte)hex2 - (byte)'0',
                        >= 'a' and <= 'f' => (byte)hex2 - (byte)'a' + 10,
                        >= 'A' and <= 'F' => (byte)hex2 - (byte)'A' + 10,
                        _ => throw new ArgumentOutOfRangeException(nameof(hex2)),
                    };

                    bytes.Add((byte)((byte1 << 4) | byte2));
                    index++;
                }
            }
            else
            {
                bytes.Add((byte)ch);
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
    public static RESULT[] ExecuteOverAll<ORIGINAL, RESULT>(
        this ORIGINAL[] orig,
        Func<ORIGINAL, RESULT> func
    )
    {
        var result = new List<RESULT>();

        foreach (var val in orig)
        {
            result.Add(func(val));
        }

        return result.ToArray();
    }

    public static RESULT[] ExecuteOverAll<ORIGINAL, RESULT>(
        this ORIGINAL[] orig,
        LLVMCompiler compiler,
        Func<LLVMCompiler, ORIGINAL, RESULT> func
    )
    {
        var result = new List<RESULT>();

        foreach (var val in orig)
        {
            result.Add(func(compiler, val));
        }

        return result.ToArray();
    }

    public static RESULT[] ExecuteOverAll<ORIGINAL, RESULT>(
        this ORIGINAL[] orig,
        LLVMCompiler compiler,
        Language lang,
        Func<LLVMCompiler, Language, ORIGINAL, RESULT> func
    )
    {
        var result = new List<RESULT>();

        foreach (var val in orig)
        {
            result.Add(func(compiler, lang, val));
        }

        return result.ToArray();
    }

    // for parsing version strings
    public static void Deconstruct(
        this string[] strings,
        out int major,
        out int minor,
        out int patch
    )
    {
        major = Int32.Parse(strings[0]);
        minor = Int32.Parse(strings[1]);
        patch = Int32.Parse(strings[2]);
    }

    public static Value[] CompileToValues(
        this ExpressionNode[] expressionNodes,
        LLVMCompiler compiler,
        Scope scope
    )
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

    public static Value[] ImplicitConvertAll(
        this Value[] values,
        LLVMCompiler compiler,
        Type target
    )
    {
        var result = new Value[values.Length];
        uint index = 0;

        foreach (Value value in values)
        {
            result[index] = value.ImplicitConvertTo(target);
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

    public static ulong[] ToULong(this byte[] bytes)
    {
        var values = new ulong[bytes.Length / 8];
        for (var i = 0; i < values.Length; i++)
            values[i] = Unsafe.ReadUnaligned<ulong>(ref bytes[i * 8]);
        return values;
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

    public static bool TryGetFunction(
        this Namespace[] imports,
        string name,
        IReadOnlyList<Type> paramTypes,
        out Function func
    )
    {
        func = null;

        foreach (var import in imports)
        {
            if (
                import.Functions.TryGetValue(name, out OverloadList overloads)
                && overloads.TryGet(paramTypes, out func)
            )
            {
                if (func is DefinedFunction defFunc && defFunc.Privacy == PrivacyType.Priv)
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

    public static bool TryGetType(this Namespace[] imports, string name, out TypeDecl typeDecl)
    {
        typeDecl = null;

        foreach (var import in imports)
        {
            if (import.Types.TryGetValue(name, out typeDecl))
            {
                if (typeDecl.Privacy == PrivacyType.Priv)
                {
                    typeDecl = null;
                }
                else
                {
                    break;
                }
            }
        }

        if (typeDecl != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool TryGetTrait(this Namespace[] imports, string name, out TraitDecl traitDecl)
    {
        traitDecl = null;

        foreach (var import in imports)
        {
            if (import.Traits.TryGetValue(name, out traitDecl))
            {
                if (traitDecl.Privacy == PrivacyType.Priv)
                {
                    traitDecl = null;
                }
                else
                {
                    break;
                }
            }
        }

        if (traitDecl != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool TryGetTemplate(this Namespace[] imports, string name, out Template template)
    {
        template = null;

        foreach (var import in imports)
        {
            if (import.Templates.TryGetValue(name, out template))
            {
                if (template.Privacy == PrivacyType.Priv)
                {
                    template = null;
                }
                else
                {
                    break;
                }
            }
        }

        if (template != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
