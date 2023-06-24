using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.Tokens
{
    internal class Token
    {
        public TokenType TokenType;
        public object Value;

        public Token(TokenType tokenType)
        {
            TokenType = tokenType;
        }

        public Token(TokenType tokenType, object value)
        {
            TokenType = tokenType;
            Value = value;
        }

        public override string ToString()
        {
            return $"({TokenType})";
        }
    }

    public enum TokenType
    {
        OpeningBracket,
        ClosingBracket,
        OpeningParentheses,
        ClosingParentheses,
        Assignment,
        Comma,
        Semicolon,
        Float,
        Int,
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
        NotAnd,
        Class,
        If,
        Var,
        New,
        Else,
        Public,
        Private,
        Void,
        Return,
        Nix,
        True,
        False,
        Num,
        Bool,
        Obj,
        Str,
        Import
    }
}
