namespace Moth.AST;

public abstract partial class ASTNode
{
    public virtual string GetSource()
    {
        throw new NotImplementedException();
    }
}
