using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LanguageParser.Token
{
    internal class IntToken : NumberToken
    {
        private int _value;

        public IntToken(int value)
        {
            _value = value;
        }

        public override string ToString()
        {
            return $"(int {_value})";
        }
    }
}
