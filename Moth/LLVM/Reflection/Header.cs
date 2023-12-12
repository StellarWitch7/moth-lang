namespace Moth.LLVM.Reflection;

public struct Header
{
    public ulong type_table_offset;
    public ulong field_table_offset;
    public ulong function_table_offset;
    public ulong method_table_offset;
    public ulong static_method_table_offset;
    public ulong global_variable_table_offset;
    public ulong functype_table_offset;
    public ulong param_table_offset;
    public ulong typeref_table_offset;
    public ulong name_table_offset;
}
