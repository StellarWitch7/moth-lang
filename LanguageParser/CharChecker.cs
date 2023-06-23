using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser
{
    public static class CharChecker
    {
        public static bool IsDigit(char? c)
        {
            return c >= '0' && c <= '9';
        }

        public static bool IsLetter(char? c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        public static bool IsVariableChar(char? c)
        {
            return IsLetter(c) || c == '_';
        }

        public static bool IsSpace(char? c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }

        public static bool IsNewLine(char? c)
        {
            return c == '\n';
        }
    }
}
