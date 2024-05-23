using Moth.AST.Node;

namespace Moth.LLVM.Metadata;

public struct Type
{
    public bool is_foreign;
    public bool is_union;
    public PrivacyType privacy;
    public ulong name_table_index;
    public ulong name_table_length;
    public ulong field_table_index;
    public ulong field_table_length;
}
