using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.AST
{
    internal class IfNode : StatementNode
    {
        public ExpressionNode Condition { get; }
        public StatementListNode Then { get; }
        public StatementListNode? Else { get; }
        public StatementListNode Continue { get; }

        public IfNode(ExpressionNode condition, StatementListNode then, StatementListNode @else, StatementListNode @continue)
        {
            Condition = condition;
            Then = then;
            Else = @else;
            Continue = @continue;
        }
    }
}
