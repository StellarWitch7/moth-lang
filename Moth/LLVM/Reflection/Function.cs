using Moth.AST.Node;

namespace Moth.LLVM.Reflection;

public struct Function
{
    public PrivacyType privacy;
    public bool is_variadic;
    public ulong type_table_index;
    public ulong name_table_index;
    public ulong name_table_length;
    public ulong param_table_index;
    public ulong param_table_length;
}
