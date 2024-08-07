﻿using System.Runtime.InteropServices;

namespace Moth.Tokens;

public readonly struct Token
{
    public required TokenType Type { get; init; }
    public ReadOnlyMemory<char> Text { get; init; }

    public int Begin
    {
        get
        {
            MemoryMarshal.TryGetString(Text, out _, out int start, out _);
            return start;
        }
    }

    public int End
    {
        get
        {
            MemoryMarshal.TryGetString(Text, out _, out int start, out int length);
            return start + length;
        }
    }

    public override string ToString() =>
        Text.IsEmpty ? $"Token<{Type}>" : $"Token<{Type}>(\"{Text}\")";
}

public enum TokenType
{
    OpeningCurlyBraces,
    ClosingCurlyBraces,
    OpeningParentheses,
    ClosingParentheses,
    Assign,
    While,
    Root,
    Constant,
    Comma,
    Semicolon,
    NamespaceSeparator,
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
    LesserThan,
    LesserThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    Equal,
    NotEqual,
    Type,
    If,
    Local,
    Else,
    Public,
    Return,
    Null,
    True,
    False,
    LiteralFloat,
    LiteralInt,
    LiteralString,
    Import,
    This,
    Decrement,
    Increment,
    OpeningSquareBrackets,
    ClosingSquareBrackets,
    For,
    In,
    Modulo,
    Not,
    Or,
    And,
    DoubleQuote,
    Foreign,
    Function,
    Range,
    Variadic,
    Static,
    AttributeMarker,
    LiteralChar,
    Then,
    TemplateTypeRef,
    AddAssign,
    SubAssign,
    MulAssign,
    DivAssign,
    ModAssign,
    ExpAssign,
    Ampersand,
    ScientificNotation,
    Trait,
    Implement,
    Union,
    Global,
    Extend,
    Enum,
    Comment,
    BlockComment
}
