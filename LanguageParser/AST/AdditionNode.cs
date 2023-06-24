using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.AST
{
    internal class AdditionNode : BinaryOperationNode
    {
        public AdditionNode(ExpressionNode left, ExpressionNode right) : base(left, right)
        {
        }
    }
}
