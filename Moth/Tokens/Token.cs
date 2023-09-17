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
	AssignmentSeparator,
	While,
	Constant,
	Comma,
	Semicolon,
	NamespaceTag,
	Name,
	Period,
	Addition,
	Subtraction,
	Multiplication,
	Division,
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
	New,
	Else,
	Public,
	Private,
	Void,
	Return,
	Null,
	True,
	False,
	Float32,
	LiteralFloat,
	Int32,
	LiteralInt,
	Bool,
	String,
	LiteralString,
	Import,
	Throw,
	This,
	Decrement,
	Increment,
    OpeningSquareBrackets,
    ClosingSquareBrackets,
	Matrix,
    For,
    In,
    Catch,
    Try,
    Modulo,
    Not,
    Xor,
    Nand,
    LogicalOr,
    LogicalXor,
    LogicalAnd,
    LogicalNand,
    DoubleQuote,
    Foreign,
    Function
}