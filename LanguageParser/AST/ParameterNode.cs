using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.AST
{
    internal class ParameterNode : ASTNode
    {
        public DefinitionType Type { get; }
        public string Name { get; }

        public ParameterNode(DefinitionType type, string name)
        {
            if (type != DefinitionType.Void)
            {
                Type = type;
                Name = name;
            }
            else
            {
                throw new ArgumentException();
            }
        }
    }
}