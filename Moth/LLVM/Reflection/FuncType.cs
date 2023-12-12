namespace Moth.LLVM.Reflection;

public struct FuncType
{
    public bool is_variadic;
    public ulong return_typeref_table_index;
    public ulong return_typeref_table_length;
    public ulong paramtype_table_index;
    public ulong paramtype_table_length;
}
