namespace Moth.AST.Node;

public class VariableRefNode : RefNode
{
    public RefNode Parent { get; }
    public bool IsLocalVar { get; }

    public VariableRefNode(string name, RefNode parent, bool isLocal = false) : base(name)
    {
        Parent = parent;
        IsLocalVar = isLocal;
    }
}