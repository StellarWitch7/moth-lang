using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Moth.Compiler.Tokens;

public sealed class UnexpectedTokenException : Exception
{
    public Token Token { get; }
    public TokenType? Expected { get; }
    public int Column { get; }
    public int Line { get; }

    public override string Message
    {
        get
        {
            var err = $"Unexpected token '{Token.Text}' at position {Token.Begin} | {Line}:{Column}.";
            if (Expected is not null) err = $"{err}\nExpected token of type {Expected}, got {Token.Type}.";
            return err;
        }
    }

    public UnexpectedTokenException(Token token, TokenType? expected = null)
    {
        Token = token;
        Expected = expected;
        if (MemoryMarshal.TryGetString(token.Text, out var text, out var start, out _))
        {
            Line = 1;
            Column = 1;
            for (var i = 0; i < start; i++)
            {
                var ch = text[i];
                if (ch == '\n')
                {
                    Line++;
                    Column = 1;
                }
                else Column++;
            }
        }
    }
}
