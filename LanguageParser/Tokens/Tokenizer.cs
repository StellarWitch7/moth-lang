using System.Text.RegularExpressions;

namespace LanguageParser.Tokens;

public static class Tokenizer
{
	public static List<Token> Tokenize(string text)
	{
		var tokens = new List<Token>(78);
		var stream = new PeekStream(text);

		while (stream.Current is {} ch)
		{
			switch (ch)
			{
				case '\n' or '\r' or '\t' or ' ': 
					break;
				
				// Skip comments
				case '/' when stream.Next is '/':
				{
					while (stream.MoveNext(out ch))
					{
						if (ch != '\n') continue;
						break;
					}
					
					break;
				}
				
				//Parse keywords or names
				case '_':
				case >= 'a' and <= 'z':
				case >= 'A' and <= 'Z':
				{
					var keyword = stream.Peek(c => char.IsLetterOrDigit(c) || c == '_');
					tokens.Add(new Token
					{
						Text = keyword,
						Type = keyword.Span switch
						{
							"if" => TokenType.If,
							"nix" => TokenType.Nix,
							"new" => TokenType.New,
							"var" => TokenType.Var,
							"num" => TokenType.Num,
							"obj" => TokenType.Obj,
							"str" => TokenType.Str,
							"set" => TokenType.Set,
							"call" => TokenType.Call,
							"bool" => TokenType.Bool,
							"true" => TokenType.True,
							"else" => TokenType.Else,
							"void" => TokenType.Void,
							"false" => TokenType.False,
							"class" => TokenType.Class,
							"import" => TokenType.Import,
							"public" => TokenType.Public,
							"return" => TokenType.Return,
							"private" => TokenType.Private,
							_ => TokenType.Name,
						},
					});

					stream.Position += keyword.Length - 1;
					break;
				}

				// Parse symbols
				case var _ when char.IsSymbol(ch) || char.IsPunctuation(ch):
				{
					var next = stream.Next;
					var type = ch switch
					{
						',' => TokenType.Comma,
						'.' => TokenType.Period,
						';' => TokenType.Semicolon,

						'{' => TokenType.OpeningBracket,
						'}' => TokenType.ClosingBracket,
						'(' => TokenType.OpeningParentheses,
						')' => TokenType.ClosingParentheses,

						'=' when next is '=' => TokenType.Equal,
						'!' when next is '=' => TokenType.NotEqual,
						'<' when next is '=' => TokenType.LessThanOrEqual,
						'>' when next is '=' => TokenType.LargerThanOrEqual,
						'>' => TokenType.LargerThan,
						'<' => TokenType.LessThan,
						
						'|' when next is '|' => TokenType.Or,
						'&' when next is '&' => TokenType.And,
						'!' when next is '&' => TokenType.NotAnd,
						
						'@' => TokenType.NamespaceTag,

						'+' => TokenType.Addition,
						'/' => TokenType.Division,
						'^' => TokenType.Exponential,
						'-' => TokenType.Subtraction,
						'*' => TokenType.Multiplication,
						'=' => TokenType.AssignmentSeparator,

						_ => throw new TokenizerException
						{
							Character = ch,
							Line = stream.CurrentLine,
							Column = stream.CurrentColumn,
							Position = stream.Position,
						},
					};
					
					tokens.Add(new Token
					{
						Text = type switch
						{
							TokenType.Equal or TokenType.NotEqual or TokenType.LessThanOrEqual or TokenType.LargerThanOrEqual or 
								TokenType.And or TokenType.NotAnd or TokenType.Or => stream.Peek(2),
							_ => stream.Peek(1),
						},
						Type = type,
					});
					
					break;
				}

				case >= '0' and <= '9':
				{
					var number = stream.Peek(c => char.IsDigit(c) || c == '.');
					tokens.Add(new Token
					{
						Text = number,
						Type = number.Span.Contains('.') ? TokenType.Float : TokenType.Int,
					});
					
					stream.Position += number.Length - 1;
					break;
				}
				
				default: throw new TokenizerException
				{
					Character = ch,
					Line = stream.CurrentLine,
					Column = stream.CurrentColumn,
					Position = stream.Position,
				};
			}

			stream.MoveNext();
		}

		return tokens;
	}
}

public sealed class TokenizerException : Exception
{
	public required char Character { get; init; }
	public required int Position { get; init; }
	public required int Column { get; init; }
	public required int Line { get; init; }
	public string? CustomMessage { get; init; }

	public override string Message => ToString();

	public override string ToString()
	{
		var ch = char.IsControl(Character) ? Regex.Escape(Character.ToString()) : Character.ToString();
		var err = $"Unexpected char '{ch}' at position {Position} | {Line}:{Column}.";
		if (CustomMessage is not null) err = $"{err}\n{CustomMessage}";
		return err;
	}
}