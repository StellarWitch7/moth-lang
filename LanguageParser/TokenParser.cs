using LanguageParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser
{
    internal class TokenParser
    {
        private ParseContext _context;

        public TokenParser(ParseContext context)
        {
            _context = context;
        }

        public void ParseTokens()
        {
            while (_context.Current != null)
            {
                
            }
        }
    }

    internal enum ParseState
    {
        None,
        Name,
        AssignToVariable,
        Expression
    }
}
