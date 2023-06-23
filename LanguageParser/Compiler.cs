using LanguageParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser
{
    internal class Compiler
    {
        private List<Token> _tokens;
        public Compiler(List<Token> tokens)
        {
            _tokens = tokens;
        }
    }
}
