using LanguageParser.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser
{
    internal class TokenParser
    {
        private ParseContext _context;

        public TokenParser(ParseContext context)
        {
            _context = context;
        }

        public StatementListNode ProcessStatementList()
        {
            List<StatementNode> statements = new List<StatementNode>();
            var current = _context.Current;

            while (current != null)
            {
                switch (current.TokenType)
                {
                    case TokenType.Set:
                        statements.Add(ProcessAssignment(_context));
                        break;
                }
            }

            return new StatementListNode(statements);
        }

        private StatementNode ProcessAssignment(ParseContext context)
        {
            throw new NotImplementedException();
        }
    }

    internal enum ParseState
    {
        None,
        Script,
        Name,
        AssignToVariable,
        Expression
    }
}
