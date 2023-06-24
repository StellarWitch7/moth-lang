using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.AST
{
    internal class AssignmentNode : StatementNode
    {
        public VariableNode Left { get; }
        public ExpressionNode Right { get; }

        public AssignmentNode(VariableNode left, ExpressionNode right)
        {
            Left = left;
            Right = right;
        }
    }
}