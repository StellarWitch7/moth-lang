using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.AST
{
    internal class CallNode : StatementNode
    {
        public MethodNode Method { get; }

        public CallNode(MethodNode method)
        {
            Method = method;
        }
    }
}
