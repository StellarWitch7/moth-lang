using Moth.Tokens;

namespace Moth.AST;

public class ParseContext
{
    public int Position { get; private set; }

    public readonly int Length;

    private readonly List<Token> _tokens;

    public ParseContext(List<Token> tokens)
    {
        _tokens = tokens;
        Length = _tokens.Count;
    }

    public Token? Current
    {
        get
        {
            if (Position >= _tokens.Count)
            {
                return null;
            }

            return _tokens[Position];
        }
    }

    public Token? MoveNext()
    {
        Position++;
        return Current;
    }
}
