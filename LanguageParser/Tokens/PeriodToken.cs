using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LanguageParser.Tokens
{
    internal class PeriodToken : Token
    {
        public override string ToString()
        {
            return $"(period)";
        }
    }
}
