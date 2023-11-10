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
            return Position >= _tokens.Count ? null : _tokens[Position];
        }
    }

    public Token? MoveNext()
    {
        Position++;
        return Current;
    }

    public Token? Previous()
    {
        Position--;
        Token? val = Current;
        Position++;
        return val;
    }
}
