using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.AST
{
    internal class StatementListNode : ASTNode
    {
        public List<StatementNode> StatementNodes { get; }

        public StatementListNode(List<StatementNode> statements)
        {
            StatementNodes = statements;
        }
    }
}
