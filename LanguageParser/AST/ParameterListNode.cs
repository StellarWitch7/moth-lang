using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.Compiler.AST
{
    public class ParameterListNode : ASTNode
    {
        public List<ParameterNode> ParameterNodes { get; }

        public ParameterListNode(List<ParameterNode> parameterNodes)
        {
            ParameterNodes = parameterNodes;
        }
    }
}
