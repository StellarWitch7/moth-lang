using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.AST
{
    internal class ParameterListNode
    {
        public List<ParameterNode> ParameterNodes;

        public ParameterListNode(List<ParameterNode> parameterNodes)
        {
            ParameterNodes = parameterNodes;
        }
    }
}
