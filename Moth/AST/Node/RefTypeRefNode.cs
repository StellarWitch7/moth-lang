namespace Moth.AST.Node;

public class RefTypeRefNode : PtrTypeRefNode
{
    public RefTypeRefNode(ITypeRefNode child)
        : base(child) { }

    public override string GetSource()
    {
        string @base = base.GetSource();
        return $"{@base.Remove(@base.Length - 1, 1)}&";
    }
}
