using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser
{
    internal class PeekStream
    {
        public readonly int Length;
        private string _content;
        private int _position;

        public PeekStream(string content)
        {
            _content = content;
            Length = _content.Length;
        }

        public char? Current
        {
            get
            {
                if (_position >= _content.Length)
                {
                    return null;
                }

                return _content[_position];
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

        public char GetByIndex(int index)
        {
            return _content[index];
        }

        public string Peek(int count)
        {
            if (_position + count <= Length)
            {
                return _content.Substring(_position, count);
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
