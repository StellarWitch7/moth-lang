using LanguageParser.Tokens;

namespace LanguageParser;

internal class ParseContext
{
	public readonly int Length;
	private readonly List<Token> _tokens;
	public int Position { get; private set; }

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
    
    public void MoveNext()
    {
        Position++;
    }

    public void MoveAmount(int amount)
    {
        Position += amount;
    }

    public Token GetByIndex(int index)
    {
        return _tokens[index];
    }

    public Token[] Peek(int count)
    {
        if (Position + count <= Length)
        {
            var copied = new Token[count];
            _tokens.CopyTo(Position, copied, 0, count);
            return copied;
        }
        else
        {
            return Array.Empty<Token>();
        }
    }
}
