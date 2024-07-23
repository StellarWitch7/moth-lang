namespace Moth.LLVM.Metadata;

public struct MetaHeader
{
    public uint type_table_offset;
    public uint field_table_offset;
    public uint function_table_offset;
    public uint global_variable_table_offset;
    public uint functype_table_offset;
    public uint param_table_offset;
    public uint paramtype_table_offset;
    public uint typeref_table_offset;
    public uint name_table_offset;
    public uint size;
}
