using Moth.AST;

namespace Moth;

public class CompilerException : Exception
{
    public IASTNode Node { get; init; }
    public string? CustomMessage { get; init; }

    public CompilerException(IASTNode node, string? customMessage)
    {
        Node = node;
        CustomMessage = customMessage;
    }

    public override string Message
    {
        get { return ToString(); }
    }

    public override string ToString()
    {
        string err =
            $"Compiling failed for node {Node.LineStart}:{Node.ColumnStart} -> {Node.LineEnd}:{Node.ColumnEnd}.";

        if (CustomMessage is not null)
            err = $"{err}\n{CustomMessage}";

        err = $"{err}\n{Node.GetSource()}";
        return err;
    }
}
