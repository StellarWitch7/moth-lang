using Moth.AST.Node;

namespace Moth.LLVM.Metadata;

public struct Global
{
    public PrivacyType privacy;
    public ulong name_table_index;
    public ulong name_table_length;
    public ulong typeref_table_index;
    public ulong typeref_table_length;
}
