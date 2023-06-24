using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser
{
    internal class ParseContext
    {
        public readonly int Length;
        private List<Token> _tokens;
        private int _position = 0;

        public ParseContext(List<Token> tokens)
        {
            _tokens = tokens;
            Length = _tokens.Count;
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

        public void MoveAmount(int amount)
        {
            _position += amount;
        }

        public Token GetByIndex(int index)
        {
            return _tokens[index];
        }

        public Token[] Peek(int count)
        {
            if (_position + count <= Length)
            {
                Token[] copied = new Token[count];
                _tokens.CopyTo(_position, copied, 0, count);
                return copied;
            }
            else
            {
                return default;
            }
        }
    }
}
