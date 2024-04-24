using Moth.AST.Node;

namespace Moth.LLVM.Metadata;

public struct Function
{
    public PrivacyType privacy;
    public bool is_method;
    public ulong name_table_index;
    public ulong name_table_length;
    public ulong typeref_table_index;
    public ulong typeref_table_length;
}
