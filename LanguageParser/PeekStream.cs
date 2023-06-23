using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser
{
    internal class PeekStream
    {
        public readonly string Content;
        private int _position;

        public PeekStream(string content)
        {
            Content = content;
        }

        public char? Current
        {
            get
            {
                if (_position >= Content.Length)
                {
                    return null;
                }

                return Content[_position];
            }
        }

        public void MoveNext()
        {
            _position++;
        }
    }
}
