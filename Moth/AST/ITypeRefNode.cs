using Moth.AST.Node;

namespace Moth.AST;

public interface ITypeRefNode : IExpressionNode
{
    //public NamespaceNode? Namespace { get; set; }

    public string GetSource(bool asChild);
}
