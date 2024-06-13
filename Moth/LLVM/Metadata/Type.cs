using Moth.AST;
using Moth.AST.Node;

namespace Moth.LLVM.Metadata;

public struct Type
{
    public bool is_foreign;
    public bool is_union;
    public PrivacyType privacy;
    public uint name_table_index;
    public uint name_table_length;
    public uint field_table_index;
    public uint field_table_length;
}
