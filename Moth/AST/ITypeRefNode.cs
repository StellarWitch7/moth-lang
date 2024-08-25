using Moth.AST.Node;

namespace Moth.AST;

public interface ITypeRefNode : IExpressionNode
{
    public string GetSource(bool asChild);
}
