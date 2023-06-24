using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.AST
{
    internal class ConstantNode : ExpressionNode
    {
        public object Value { get; }

        public ConstantNode(object value)
        {
            Value = value;
        }
    }
}
