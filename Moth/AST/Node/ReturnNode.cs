namespace Moth.AST.Node;

public class ReturnNode : StatementNode
{
    public ExpressionNode? ReturnValue { get; set; }

    public ReturnNode(ExpressionNode returnValue) => ReturnValue = returnValue;
}
