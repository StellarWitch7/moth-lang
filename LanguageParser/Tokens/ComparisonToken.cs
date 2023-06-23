using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.Tokens
{
    internal class ComparisonToken : Token
    {
        private ComparisonType _type;

        public ComparisonToken(ComparisonType type)
        {
            _type = type;
        }
        public override string ToString()
        {
            return $"(comparison <{_type}>)";
        }
    }

    internal enum ComparisonType
    {
        LessThan,
        LessThanOrEqual,
        LargerThan,
        LargerThanOrEqual,
        Equal,
        NotEqual,
        And,
        Or,
        NotAnd
    }
}
