using System.Runtime.InteropServices;

namespace Moth.Tokens;

public readonly struct Token
{
	public required TokenType Type { get;  init; }
	public ReadOnlyMemory<char> Text { get; init; }

	public int Begin
	{
		get
		{
			MemoryMarshal.TryGetString(Text, out _, out var start, out _);
			return start;
		}
	}
	
	public int End
	{
		get
		{
			MemoryMarshal.TryGetString(Text, out _, out var start, out var length);
			return start + length;
		}
	}

	public override string ToString() => Text.IsEmpty
		? $"Token<{Type}>"
		: $"Token<{Type}>(\"{Text}\")";
}

public enum TokenType
{
	OpeningCurlyBraces,
	ClosingCurlyBraces,
	OpeningParentheses,
	ClosingParentheses,
	Assign,
	While,
	Constant,
	Comma,
	Semicolon,
	Namespace,
	Name,
	Period,
	TypeRef,
	Colon,
	Plus,
	Hyphen,
	Asterix,
	ForwardSlash,
	Exponential,
	LessThan,
	LessThanOrEqual,
	LargerThan,
	LargerThanOrEqual,
	Equal,
	NotEqual,
	And,
	Or,
	Class,
	If,
	Local,
	Else,
	Public,
	Private,
	Return,
	Null,
	True,
	False,
	LiteralFloat,
	LiteralInt,
	LiteralString,
	Import,
	Throw,
	This,
	Decrement,
	Increment,
    OpeningSquareBrackets,
    ClosingSquareBrackets,
    For,
    In,
    Catch,
    Try,
    Modulo,
    Not,
    LogicalOr,
    LogicalAnd,
    DoubleQuote,
    Foreign,
    Function,
	Pi,
    Range,
    Variadic,
    DollarSign,
    Static,
    InferAssign
}