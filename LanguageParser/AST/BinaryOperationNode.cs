using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.AST
{
    internal class BinaryOperationNode : ExpressionNode
    {
        public ExpressionNode Left { get; }
        public ExpressionNode Right { get; }

        public BinaryOperationNode(ExpressionNode left, ExpressionNode right)
        {
            Left = left;
            Right = right;
        }
    }

    public enum OperationType
    {
        Addition,
        Subtraction,
        Multiplication,
        Division,
        Exponential
    }
}
