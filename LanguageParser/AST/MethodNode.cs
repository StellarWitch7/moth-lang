using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.AST
{
    internal class MethodNode : ExpressionNode
    {
        public string Name { get; }
        public string OriginClass { get; }
        public List<ExpressionNode> Arguments { get; }

        public MethodNode(string name, string originClass, List<ExpressionNode> arguments)
        {
            Name = name;
            OriginClass = originClass;
            Arguments = arguments;
        }
    }
}
