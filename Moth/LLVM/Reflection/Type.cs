using Moth.AST.Node;

namespace Moth.LLVM.Reflection;

public struct Type
{
    public PrivacyType privacy;
    public ulong name_table_index;
    public ulong name_table_length;
    public ulong field_table_index;
    public ulong field_table_length;
}
