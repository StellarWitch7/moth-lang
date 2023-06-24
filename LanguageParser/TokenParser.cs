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

            while (_context.Current != null)
            {
                switch (_context.Current.TokenType)
                {
                    case TokenType.Semicolon:
                        _context.MoveNext();
                        break;
                    case TokenType.Set:
                        _context.MoveNext();
                        statements.Add(ProcessAssignment());
                        break;
                    default: break;
                }
            }

            return new StatementListNode(statements);
        }

        public AssignmentNode ProcessAssignment()
        {
            var newVarNode = new VariableNode((string)_context.Current.Value);
            _context.MoveNext();
            _context.MoveNext();
            var newExprNode = ProcessExpression();
            return new AssignmentNode(newVarNode, newExprNode);
        }

        private ExpressionNode ProcessExpression()
        {
            var newExprNode = new ExpressionNode();

            while (_context.Current != null && _context.Current.TokenType != TokenType.Semicolon)
            {
                switch (_context.Current.TokenType)
                {
                    case TokenType.Float:
                        newExprNode = new ConstantNode(_context.Current.Value);
                        _context.MoveNext();
                        break;
                    case TokenType.Int:
                        newExprNode = new ConstantNode(_context.Current.Value);
                        _context.MoveNext();
                        break;
                    default:
                        Console.WriteLine($"Expected expression after keyword {TokenType.Set}.");
                        return default;
                }
            }

            return newExprNode;
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
