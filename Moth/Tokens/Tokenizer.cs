﻿using System.Text.RegularExpressions;
using Moth.LLVM;

namespace Moth.Tokens;

public static class Tokenizer
{
    public static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>(78);
        var stream = new PeekStream(text);

        while (stream.Current is { } ch)
        {
            switch (ch)
            {
                case '\n'
                or '\r'
                or '\t'
                or ' ':
                    break;

                // Capture comments
                case '/' when stream.Next is '/':
                {
                    var builder = new StringBuilder();
                    stream.Position++;

                    while (stream.MoveNext(out ch))
                    {
                        if (ch == '\n')
                            break;

                        builder.Append(ch);
                    }

                    tokens.Add(
                        new Token()
                        {
                            Type = TokenType.Comment,
                            Text = builder.ToString().AsMemory(),
                        }
                    );
                    break;
                }

                case '/' when stream.Next is '>':
                {
                    var builder = new StringBuilder();
                    stream.Position++;

                    while (stream.MoveNext(out ch))
                    {
                        if (ch == '<' && stream.Next == '/')
                            break;

                        builder.Append(ch);
                    }

                    tokens.Add(
                        new Token()
                        {
                            Type = TokenType.BlockComment,
                            Text = builder.ToString().AsMemory()
                        }
                    );
                    stream.Position++;
                    break;
                }

                case 'e' when stream.Next is '+':
                {
                    stream.Position++;
                    tokens.Add(
                        new Token() { Type = TokenType.ScientificNotation, Text = "e+".AsMemory(), }
                    );
                    break;
                }

                //Parse character constants
                case '\'':
                {
                    stream.Position++;

                    if (stream.Current == '\'')
                    {
                        throw new TokenizerException()
                        {
                            Character = (char)stream.Current,
                            Line = stream.CurrentLine,
                            Column = stream.CurrentColumn,
                            Position = stream.Position,
                        };
                    }
                    else
                    {
                        tokens.Add(
                            new Token()
                            {
                                Type = TokenType.LiteralChar,
                                Text = $"{ProcessCharacter(ref stream)}".AsMemory(),
                            }
                        );

                        stream.Position++;

                        if (stream.Current == '\'')
                        {
                            break;
                        }
                        else
                        {
                            throw new TokenizerException()
                            {
                                Character = (char)stream.Current,
                                Line = stream.CurrentLine,
                                Column = stream.CurrentColumn,
                                Position = stream.Position,
                            };
                        }
                    }
                }

                //Parse keywords or names
                case >= 'a'
                and <= 'z':
                case >= 'A'
                and <= 'Z':
                case '_':
                {
                    ReadOnlyMemory<char> keyword = stream.Peek(c =>
                        char.IsLetterOrDigit(c) || c == '_'
                    );

                    if (keyword.Span.ToString() == "op")
                    {
                        stream.Position += keyword.Length - 1;

                        if (stream.Next != '{')
                        {
                            throw new Exception();
                        }

                        stream.Position++;

                        char? current = stream.Next;
                        char? next = stream.Next2;
                        string name;

                        switch (current)
                        {
                            case '.' when next is '.':
                            case '=' when next is '=':
                            case '<' when next is '=':
                            case '>' when next is '=':
                                name = Utils.ExpandOpName($"{current}{next}");
                                stream.Position += 2;
                                break;
                            case '^':
                            case '>':
                            case '<':
                            case '+':
                            case '/':
                            case '-':
                            case '*':
                            case '%':
                                name = Utils.ExpandOpName($"{current}");
                                stream.Position++;
                                break;
                            default:
                                throw new Exception();
                        }
                        ;

                        if (stream.Next != '}')
                        {
                            throw new Exception();
                        }

                        stream.Position++;

                        tokens.Add(new Token { Text = name.AsMemory(), Type = TokenType.Name, });

                        break;
                    }
                    else
                    {
                        tokens.Add(
                            new Token
                            {
                                Text = keyword,
                                Type = keyword.Span switch
                                {
                                    Reserved.If => TokenType.If,
                                    Reserved.Null => TokenType.Null,
                                    Reserved.Var => TokenType.Local,
                                    Reserved.Self => TokenType.This,
                                    Reserved.Extend => TokenType.Extend,
                                    Reserved.Namespace => TokenType.Namespace,
                                    Reserved.Then => TokenType.Then,
                                    Reserved.Constant => TokenType.Constant,
                                    Reserved.While => TokenType.While,
                                    Reserved.True => TokenType.True,
                                    Reserved.Else => TokenType.Else,
                                    Reserved.False => TokenType.False,
                                    Reserved.Enum => TokenType.Enum,
                                    Reserved.For => TokenType.For,
                                    Reserved.In => TokenType.In,
                                    Reserved.Or => TokenType.Or,
                                    Reserved.And => TokenType.And,
                                    Reserved.Root => TokenType.Root,
                                    Reserved.Global => TokenType.Global,
                                    Reserved.Function => TokenType.Function,
                                    Reserved.Type => TokenType.Type,
                                    Reserved.Union => TokenType.Union,
                                    Reserved.Implement => TokenType.Implement,
                                    Reserved.Trait => TokenType.Trait,
                                    Reserved.With => TokenType.Import,
                                    Reserved.Public => TokenType.Public,
                                    Reserved.Static => TokenType.Static,
                                    Reserved.Return => TokenType.Return,
                                    Reserved.Foreign => TokenType.Foreign,
                                    _ => TokenType.Name,
                                },
                            }
                        );

                        stream.Position += keyword.Length - 1;
                        break;
                    }
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
                        else
                        {
                            builder.Append(ProcessCharacter(ref stream));
                            stream.Position++;
                        }
                    }

                    string @string = builder.ToString();
                    tokens.Add(
                        new Token { Text = @string.AsMemory(), Type = TokenType.LiteralString }
                    );

                    break;
                }

                case '#'
                    when char.IsLetter((char)stream.Next)
                        || (char)stream.Next == '('
                        || (char)stream.Next == '[':
                case '?' when char.IsLetter((char)stream.Next):
                {
                    char character = (char)stream.Current;
                    tokens.Add(
                        new Token()
                        {
                            Text = $"{character}".AsMemory(),
                            Type = character == '?' ? TokenType.TemplateTypeRef : TokenType.TypeRef,
                        }
                    );

                    break;
                }

                // Parse symbols
                case var _ when char.IsSymbol(ch) || char.IsPunctuation(ch):
                {
                    char? next = stream.Next;
                    char? next2 = stream.Next2;
                    TokenType? type;

                    if (ch == '.' && next == '.' && next2 == '.')
                    {
                        type = TokenType.Variadic;
                    }
                    else
                    {
                        type = ch switch
                        {
                            '.' when next is '.' => TokenType.Range,
                            '=' when next is '=' => TokenType.Equal,
                            '!' when next is '=' => TokenType.NotEqual,
                            '<' when next is '=' => TokenType.LesserThanOrEqual,
                            '>' when next is '=' => TokenType.GreaterThanOrEqual,
                            '+' when next is '=' => TokenType.AddAssign,
                            '-' when next is '=' => TokenType.SubAssign,
                            '*' when next is '=' => TokenType.MulAssign,
                            '/' when next is '=' => TokenType.DivAssign,
                            '%' when next is '=' => TokenType.ModAssign,
                            '^' when next is '=' => TokenType.ExpAssign,
                            '+' when next is '+' => TokenType.Increment,
                            '-' when next is '-' => TokenType.Decrement,
                            ':' when next is ':' => TokenType.NamespaceSeparator,
                            '&' => TokenType.Ampersand,
                            ':' => TokenType.Colon,
                            '^' => TokenType.Exponential,
                            ',' => TokenType.Comma,
                            '.' => TokenType.Period,
                            ';' => TokenType.Semicolon,
                            '{' => TokenType.OpeningCurlyBraces,
                            '}' => TokenType.ClosingCurlyBraces,
                            '(' => TokenType.OpeningParentheses,
                            ')' => TokenType.ClosingParentheses,
                            '[' => TokenType.OpeningSquareBrackets,
                            ']' => TokenType.ClosingSquareBrackets,
                            '>' => TokenType.GreaterThan,
                            '<' => TokenType.LesserThan,
                            '!' => TokenType.Not,
                            '+' => TokenType.Plus,
                            '/' => TokenType.ForwardSlash,
                            '-' => TokenType.Hyphen,
                            '*' => TokenType.Asterix,
                            '%' => TokenType.Modulo,
                            '=' => TokenType.Assign,
                            '@' => TokenType.AttributeMarker,

                            _
                                => throw new TokenizerException
                                {
                                    Character = ch,
                                    Line = stream.CurrentLine,
                                    Column = stream.CurrentColumn,
                                    Position = stream.Position,
                                },
                        };
                    }

                    var newToken = new Token
                    {
                        Text = type switch
                        {
                            TokenType.Variadic => stream.Peek(3),
                            TokenType.NamespaceSeparator
                            or TokenType.AddAssign
                            or TokenType.SubAssign
                            or TokenType.MulAssign
                            or TokenType.DivAssign
                            or TokenType.ModAssign
                            or TokenType.ExpAssign
                            or TokenType.Increment
                            or TokenType.Decrement
                            or TokenType.LesserThanOrEqual
                            or TokenType.GreaterThanOrEqual
                            or TokenType.Equal
                            or TokenType.NotEqual
                                => stream.Peek(2),
                            _ => stream.Peek(1),
                        },
                        Type = (TokenType)type,
                    };

                    tokens.Add(newToken);
                    stream.Position += newToken.Text.Length - 1;
                    break;
                }

                case >= '0'
                and <= '9':
                {
                    var builder = new StringBuilder();

                    while (
                        char.IsDigit((char)stream.Current)
                        || (char)stream.Current == '.'
                        || (char)stream.Current == '_'
                    )
                    {
                        if ((char)stream.Current != '_')
                        {
                            builder.Append((char)stream.Current);
                        }

                        stream.Position++;
                    }

                    ReadOnlyMemory<char> number = builder.ToString().AsMemory();
                    int dots = 0;
                    ReadOnlySpan<char> numberSpan = number.Span;
                    for (int i = 0; i < numberSpan.Length; i++)
                    {
                        if (numberSpan[i] == '.')
                        {
                            dots++;
                        }

                        if (dots >= 2)
                        {
                            throw new TokenizerException
                            {
                                Character = numberSpan[i],
                                Position = stream.Position + i + 1,
                                Column = stream.CurrentColumn + i + 1,
                                Line = stream.CurrentLine,
                            };
                        }
                    }

                    tokens.Add(
                        new Token
                        {
                            Text = number,
                            Type = number.Span.Contains('.')
                                ? TokenType.LiteralFloat
                                : TokenType.LiteralInt,
                        }
                    );

                    stream.Position--;
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

    public static char? ProcessCharacter(ref PeekStream stream)
    {
        if (stream.Current == '\\')
        {
            stream.Position++;
            return stream.Current switch
            {
                '0' => '\0',
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                'b' => '\b',
                '\\' => '\\',
                '"' => '"',
                '\'' => '\'',
                _
                    => throw new TokenizerException()
                    {
                        Character = (char)stream.Next,
                        Line = stream.CurrentLine,
                        Column = stream.CurrentColumn,
                        Position = stream.Position,
                    },
            };
        }
        else
        {
            return stream.Current;
        }
    }
}

public sealed class TokenizerException : Exception
{
    public required char Character { get; init; }
    public required int Position { get; init; }
    public required int Column { get; init; }
    public required int Line { get; init; }
    public string? CustomMessage { get; init; }

    public override string Message
    {
        get { return ToString(); }
    }

    public override string ToString()
    {
        string ch = char.IsControl(Character)
            ? Regex.Escape(Character.ToString())
            : Character.ToString();
        string err = $"Unexpected char '{ch}' at position {Position} | {Line}:{Column}.";
        if (CustomMessage is not null)
        {
            err = $"{err}\n{CustomMessage}";
        }

        return err;
    }
}
