using System.Text;
using System.Text.RegularExpressions;

namespace Moth.Tokens;

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
				
				//Skip comments
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
                                    "null" => TokenType.Null,
									"local" => TokenType.Local,
									"self" => TokenType.This,
									"namespace" => TokenType.Namespace,
									"throw" => TokenType.Throw,
									"constant" => TokenType.Constant,
									"while" => TokenType.While,
									"true" => TokenType.True,
									"pi" => TokenType.Pi,
									"else" => TokenType.Else,
									"false" => TokenType.False,
									"every" => TokenType.For,
									"in" => TokenType.In,
									"func" => TokenType.Function,
                                    "attempt" => TokenType.Try,
									"seize" => TokenType.Catch,
									"class" => TokenType.Class,
									"wield" => TokenType.Import,
									"public" => TokenType.Public,
									"static" => TokenType.Static,
									"return" => TokenType.Return,
									"private" => TokenType.Private,
									"foreign" => TokenType.Foreign,
									_ => TokenType.Name,
								},
							});

						stream.Position += keyword.Length - 1;
						break;
					}

				//Parse strings
                case '"':
                    {
                        stream.Position++;
						var builder = new StringBuilder();

						while (stream.Current != null)
						{
							if (stream.Current == '"')
							{
								break;
							}

                            if (stream.Current == '\\')
                            {
                                stream.Position++;
                            }

                            builder.Append(stream.Current);
							stream.Position++;
						}

						string @string = builder.ToString();
                        tokens.Add(new Token
                        {
                            Text = @string.AsMemory(),
                            Type = TokenType.LiteralString
                        });

                        break;
                    }

				case '#':
					{
                        if (char.IsLetter((char)stream.Next))
                        {
                            tokens.Add(new Token()
                            {
                                Text = "#".AsMemory(),
                                Type = TokenType.TypeRef,
                            });

                            break;
                        }
                        else
                        {
                            throw new TokenizerException
                            {
                                Character = (char)stream.Next,
                                Line = stream.CurrentLine,
                                Column = stream.CurrentColumn,
                                Position = stream.Position,
                            };
                        }
                    }

                // Parse symbols
                case var _ when char.IsSymbol(ch) || char.IsPunctuation(ch):
					{
						var next = stream.Next;
						TokenType? type = ch switch
						{
                            '.' when next is '.' => TokenType.Range,
							'=' when next is '=' => TokenType.Equal,
							'!' when next is '=' => TokenType.NotEqual,
							'<' when next is '=' => TokenType.LessThanOrEqual,
							'>' when next is '=' => TokenType.LargerThanOrEqual,
							'+' when next is '+' => TokenType.Increment,
							'-' when next is '-' => TokenType.Decrement,
							'*' when next is '^' => TokenType.Exponential,
							'|' when next is '|' => TokenType.LogicalOr,
							'&' when next is '&' => TokenType.LogicalAnd,
							'~' when next is '~' => TokenType.Variadic,
							':' when next is '=' => TokenType.InferAssign,
							':' => TokenType.Colon,
							',' => TokenType.Comma,
							'.' => TokenType.Period,
							';' => TokenType.Semicolon,
							'{' => TokenType.OpeningCurlyBraces,
							'}' => TokenType.ClosingCurlyBraces,
							'(' => TokenType.OpeningParentheses,
							')' => TokenType.ClosingParentheses,
							'[' => TokenType.OpeningSquareBrackets,
							']' => TokenType.ClosingSquareBrackets,
							'>' => TokenType.LargerThan,
							'<' => TokenType.LessThan,
							'|' => TokenType.Or,
							'&' => TokenType.And,
							'!' => TokenType.Not,
							'$' => TokenType.DollarSign,
							'+' => TokenType.Plus,
							'/' => TokenType.ForwardSlash,
							'-' => TokenType.Hyphen,
							'*' => TokenType.Asterix,
							'%' => TokenType.Modulo,
							'=' => TokenType.Assign,

							_ => throw new TokenizerException
							{
								Character = ch,
								Line = stream.CurrentLine,
								Column = stream.CurrentColumn,
								Position = stream.Position,
							},
						};

						var newToken = new Token
						{
							Text = type switch
							{
								TokenType.Equal or TokenType.NotEqual or TokenType.LessThanOrEqual
								    or TokenType.LargerThanOrEqual or TokenType.LogicalAnd
									or TokenType.LogicalOr or TokenType.Exponential
									or TokenType.Increment or TokenType.Decrement
									or TokenType.Variadic or TokenType.InferAssign => stream.Peek(2),
								_ => stream.Peek(1),
							},
							Type = (TokenType)type,
						};

						tokens.Add(newToken);
						stream.Position += newToken.Text.Length - 1;
						break;
					}

				case >= '0' and <= '9':
					{
						var number = stream.Peek(c => char.IsDigit(c) || c == '.');
						var dots = 0;
						var numberSpan = number.Span;
						for (var i = 0; i < numberSpan.Length; i++)
						{
							if (numberSpan[i] == '.') dots++;
							if (dots >= 2)
								throw new TokenizerException
								{
									Character = numberSpan[i],
									Position = stream.Position + i + 1,
									Column = stream.CurrentColumn + i + 1,
									Line = stream.CurrentLine,
								};
						}

						tokens.Add(new Token
						{
							Text = number,
							Type = number.Span.Contains('.') ? TokenType.LiteralFloat : TokenType.LiteralInt,
						});

						stream.Position += number.Length - 1;
						break;
					}
				
				default:
					throw new TokenizerException
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