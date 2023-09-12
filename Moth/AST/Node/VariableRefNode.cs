namespace Moth.AST.Node;

public class VariableRefNode : RefNode
{
    public bool IsLocalVar { get; }

    public VariableRefNode(string name, bool isLocal = false) : base(name)
    {
        IsLocalVar = isLocal;
    }
}