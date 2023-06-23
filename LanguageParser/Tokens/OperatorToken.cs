using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LanguageParser.Tokens
{
    internal class OperatorToken : Token
    {
        private Operator _operator;

        public OperatorToken(Operator op)
        {
            _operator = op;
        }

        public override string ToString()
        {
            return $"(operator <{_operator}>)";
        }
    }

    internal enum Operator
    {
        Addition,
        Subtraction,
        Multiplication,
        Division,
        Exponential
    }
}
