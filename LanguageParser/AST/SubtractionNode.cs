using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.AST
{
    internal class SubtractionNode : BinaryOperationNode
    {
        public SubtractionNode(ExpressionNode left, ExpressionNode right) : base(left, right)
        {
        }
    }
}
