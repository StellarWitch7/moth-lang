using Moth.AST.Node;

namespace Moth.LLVM.Reflection;

public struct Field
{
    public PrivacyType privacy;
    public ulong name_table_index;
    public ulong name_table_length;
    public ulong type_table_index;
}
