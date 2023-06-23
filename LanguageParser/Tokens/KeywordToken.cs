using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.Tokens
{
    internal class KeywordToken : Token
    {
        private Keyword _keyword;

        public KeywordToken(Keyword keyword)
        {
            _keyword = keyword;
        }

        public override string ToString()
        {
            return $"(keyword <{_keyword}>)";
        }
    }

    internal enum Keyword
    {
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
        False
    }
}
