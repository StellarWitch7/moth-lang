namespace Moth.LLVM.Metadata;

public struct FuncType
{
    public bool is_variadic;
    public uint return_typeref_table_index;
    public uint return_typeref_table_length;
    public uint paramtype_table_index;
    public uint paramtype_table_length;
}
