namespace Moth.AST.Node;

public abstract class MemberContainingDefinitionNode : DefinitionNode
{
    protected List<IMemberDeclNode>? _contents;

    public MemberContainingDefinitionNode(
        string name,
        PrivacyType privacy,
        List<IMemberDeclNode>? contents,
        List<AttributeNode>? attributes
    )
        : base(name, privacy, attributes)
    {
        _contents = contents;
    }

    public bool IsEmpty
    {
        get => _contents == null;
    }

    public FieldDefNode[] Fields
    {
        get { return IsEmpty ? new FieldDefNode[0] : _contents.OfType<FieldDefNode>().ToArray(); }
    }

    public FuncDefNode[] Functions
    {
        get { return IsEmpty ? new FuncDefNode[0] : _contents.OfType<FuncDefNode>().ToArray(); }
    }

    public DefinitionNode[] OrganizedMembers
    {
        get
        {
            var result = new List<DefinitionNode>();

            result.AddRange(Fields);
            result.AddRange(Functions);

            return result.ToArray();
        }
    }
}
