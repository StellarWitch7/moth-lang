using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LanguageParser.Token
{
    internal class FloatToken : NumberToken
    {
        private float _value;

        public FloatToken(float value)
        {
            _value = value;
        }

        public override string ToString()
        {
            return $"(float {_value})";
        }
    }
}
