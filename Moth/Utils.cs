using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Moth.AST;
using Moth.AST.Node;

namespace Moth;

public static class Utils
{
    public static List<T> Combine<T>(params IEnumerable<T>[] listList)
    {
        var result = new List<T>();

        foreach (var list in listList)
        {
            result.AddRange(list);
        }

        return result;
    }

    public static CompressionLevel StringToCompLevel(string str)
    {
        return str switch
        {
            "none" => CompressionLevel.NoCompression,
            "low" => CompressionLevel.Fastest,
            "mid" => CompressionLevel.Optimal,
            "high" => CompressionLevel.SmallestSize,
            _ => throw new NotImplementedException($"Unsupported compression type: \"{str}\"")
        };
    }

    public static string ExpandOpName(string op)
    {
        return $"{Reserved.Operator}{{{op}}}";
    }

    public static string OpTypeToString(OperationType opType)
    {
        return opType switch
        {
            OperationType.Assignment => "=",
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
            OperationType.NotEqual => "!=",
            OperationType.And => Reserved.And,
            OperationType.Or => Reserved.Or,
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
}
