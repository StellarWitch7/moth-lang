using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.Token
{
    internal class AssignmentToken : ParsedToken
    {
        public override string ToString()
        {
            return $"(assignment)";
        }
    }
}
