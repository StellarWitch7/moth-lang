﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.Tokens
{
    internal class OpeningToken : Token
    {
        public override string ToString()
        {
            return $"(opening bracket)";
        }
    }
}
