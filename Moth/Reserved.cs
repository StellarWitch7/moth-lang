using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public static class Reserved
{
    public static readonly string Main = "main";
    public static readonly string Self = "self";
    public static readonly string Init = "init";
    public static readonly string SizeOf = "sizeof";
    public static readonly string AlignOf = "alignof";

    public static readonly string Void = "void";
    public static readonly string Float16 = "f16";
    public static readonly string Float32 = "f32";
    public static readonly string Float64 = "f64";
    public static readonly string Bool = "bool";
    public static readonly string Char = "char";
    public static readonly string UnsignedInt16 = "u16";
    public static readonly string UnsignedInt32 = "u32";
    public static readonly string UnsignedInt64 = "u64";
    public static readonly string SignedInt8 = "i8";
    public static readonly string SignedInt16 = "i16";
    public static readonly string SignedInt32 = "i32";
    public static readonly string SignedInt64 = "i64";
}
