namespace Moth.LLVM;

public static class Reserved
{
    // keywords and symbols
    public const string Namespace = "namespace";
    public const string Var = "var";
    public const string Then = "then";
    public const string If = "if";
    public const string Else = "else";
    public const string While = "while";
    public const string For = "for";
    public const string In = "in";
    public const string Or = "or";
    public const string And = "and";
    public const string Root = "root";
    public const string Implement = "impl";
    public const string Trait = "trait";
    public const string With = "with";
    public const string Static = "static";
    public const string Foreign = "foreign";
    public const string Return = "ret";
    public const string Global = "global";
    public const string Function = "fn";
    public const string Type = "type";
    public const string Enum = "enum";
    public const string Union = "union";
    public const string Constant = "const";
    public const string Public = "pub";
    public const string Extend = "extend";
    public const string Variadic = "...";

    // function names
    public const string Main = "main";
    public const string Init = "init";
    public const string Indexer = "indxr";
    public const string Malloc = "malloc";
    public const string Realloc = "realloc";
    public const string Free = "free";
    public const string SizeOf = "sizeof";
    public const string AlignOf = "alignof";
    public const string LocalFunc = "localfunc";
    public const string Operator = "__operator";

    // types
    public const string Void = "void";
    public const string Bool = "bool";
    public const string UInt8 = "u8";
    public const string UInt16 = "u16";
    public const string UInt32 = "u32";
    public const string UInt64 = "u64";
    public const string UInt128 = "u128";
    public const string Int8 = "i8";
    public const string Int16 = "i16";
    public const string Int32 = "i32";
    public const string Int64 = "i64";
    public const string Int128 = "i128";
    public const string Float16 = "f16";
    public const string Float32 = "f32";
    public const string Float64 = "f64";

    // values
    public const string Null = "null";
    public const string Self = "self";
    public const string True = "true";
    public const string False = "false";

    // attributes
    public const string Export = "Export";
    public const string CallConv = "CallConv";
    public const string TargetOS = "TargetOS";

    // operating systems
    public const string Windows = "windows";
    public const string Linux = "linux";
    public const string MacOS = "macos";

    // languages
    public const string C = "c";
    public const string CPP = "cpp";
}
