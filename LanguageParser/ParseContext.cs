using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser
{
    internal class ParseContext
    {
        public Stack<ParseState> StateStack;
        private List<Token> _tokens;
        private int _position;

        public ParseContext(List<Token> tokens)
        {
            _tokens = tokens;
        }

        public Token? Current
        {
            get
            {
                if (_position >= _tokens.Count)
                {
                    return null;
                }

                return _tokens[_position];
            }
        }

        public int Position
        {
            get
            {
                return _position;
            }
        }

        public void MoveNext()
        {
            _position++;
        }
    }
}
