using Moth.AST.Node;

namespace Moth.LLVM.Reflection;

public struct Function
{
    public PrivacyType privacy;
    public ulong name_table_index;
    public ulong name_table_length;
    public ulong functype_table_index;
}
