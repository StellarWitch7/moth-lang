using Moth.AST;
using Moth.AST.Node;

namespace Moth.LLVM.Metadata;

public struct MetaFunction
{
    public PrivacyType privacy;
    public bool is_method;
    public uint name_table_index;
    public uint name_table_length;
    public uint typeref_table_index;
    public uint typeref_table_length;
}
