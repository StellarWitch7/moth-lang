using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.AST
{
    internal class ClassRefNode : ASTNode
    {
        public string Name { get; }

        public ClassRefNode(string name)
        {
            Name = name;
        }
    }
}
