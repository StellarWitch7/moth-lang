namespace Moth.Compiler.Tokens;

public struct PeekStream
{
	public int Position;
	private readonly string _text;
	public int Length => _text.Length;

	public PeekStream() : this(string.Empty)
	{
		Position = 0;
	}

	public PeekStream(string text)
	{
		_text = text;
		Position = 0;
	}

	public char? Current => Position < Length
		? _text[Position]
		: null;
	
	public char? Next => Position < Length - 1
		? _text[Position + 1]
		: null;

	public bool MoveNext()
	{
		Position++;
		return Position < Length;
	}
	
	public bool MoveNext(out char nextChar)
	{
		Position++;
		if (Current is {} ch)
		{
			nextChar = ch;
			return true;
		}
		else
		{
			nextChar = default;
			return false;
		}
	}

	public ReadOnlyMemory<char> Peek(int count) => Position + count <= Length ? _text.AsMemory(Position, count) : default;

	public ReadOnlyMemory<char> Peek(Func<char, bool> condition)
	{
		var span = _text.AsSpan(Position);
		for (var i = 0; i < span.Length; i++)
		{
			if(condition(span[i])) continue;
			return _text.AsMemory(Position, i);
		}

		return _text.AsMemory();
	}

	public int CurrentLine
	{
		get
		{
			var count = 1;
			foreach (var ch in _text.AsSpan(0, Position))
				if (ch == '\n') count++;

			return count;
		}
	}
	
	public int CurrentColumn
	{
		get
		{
			var column = 1;
			foreach (var ch in _text.AsSpan(0, Position))
			{
				if (ch == '\n') column = 1;
				else column++;
			}

			return column;
		}
	}
}