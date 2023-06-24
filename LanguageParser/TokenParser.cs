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
                    case TokenType.Set:
                        _context.MoveNext();
                        statements.Add(ProcessAssignment());
                        break;
                    default:
                        _context.MoveNext();
                        break;
                }
            }

            return new StatementListNode(statements);
        }

        private AssignmentNode ProcessAssignment()
        {
            var newVarNode = new VariableNode((string)_context.Current.Value);
            _context.MoveAmount(2);
            var newExprNode = ProcessExpression();
            return new AssignmentNode(newVarNode, newExprNode);
        }

        private ExpressionNode ProcessExpression()
        {
            ExpressionNode newExprNode = default;
            bool exprFound = false;

            while (_context.Current != null && _context.Current.TokenType != TokenType.Semicolon)
            {
                switch (_context.Current.TokenType)
                {
                    case TokenType.Semicolon:
                        _context.MoveNext();

                        if (!exprFound)
                        {
                            Console.WriteLine($"Expected expression after keyword {TokenType.Set}.");
                        }

                        return newExprNode;
                    case TokenType.Float:
                        newExprNode = new ConstantNode(_context.Current.Value);
                        _context.MoveNext();
                        exprFound = true;
                        break;
                    case TokenType.Int:
                        newExprNode = new ConstantNode(_context.Current.Value);
                        _context.MoveNext();
                        exprFound = true;
                        break;
                    case TokenType.Addition:
                        if (newExprNode != null)
                        {
                            newExprNode = ProcessBinaryOp(OperationType.Addition, newExprNode);
                        }
                        else
                        {
                            Console.WriteLine($"Failed to parse expression after keyword {TokenType.Set}.");
                        }
                        break;
                    case TokenType.Subtraction:
                        if (newExprNode != null)
                        {
                            newExprNode = ProcessBinaryOp(OperationType.Subtraction, newExprNode);
                        }
                        else
                        {
                            Console.WriteLine($"Failed to parse expression after keyword {TokenType.Set}.");
                        }
                        break;
                    case TokenType.Multiplication:
                        if (newExprNode != null)
                        {
                            newExprNode = ProcessBinaryOp(OperationType.Multiplication, newExprNode);
                        }
                        else
                        {
                            Console.WriteLine($"Failed to parse expression after keyword {TokenType.Set}.");
                        }
                        break;
                    case TokenType.Division:
                        if (newExprNode != null)
                        {
                            newExprNode = ProcessBinaryOp(OperationType.Division, newExprNode);
                        }
                        else
                        {
                            Console.WriteLine($"Failed to parse expression after keyword {TokenType.Set}.");
                        }
                        break;
                    case TokenType.Exponential:
                        if (newExprNode != null)
                        {
                            newExprNode = ProcessBinaryOp(OperationType.Exponential, newExprNode);
                        }
                        else
                        {
                            Console.WriteLine($"Failed to parse expression after keyword {TokenType.Set}.");
                        }
                        break;
                    default:
                        Console.WriteLine($"Failed to parse expression after keyword {TokenType.Set}.");
                        return default;
                }
            }

            if (!exprFound)
            {
                Console.WriteLine($"Expected expression after keyword {TokenType.Set}.");
            }

            return newExprNode;
        }

        private ExpressionNode ProcessBinaryOp(OperationType opType, ExpressionNode left)
        {
            _context.MoveNext();
            var right = ProcessExpression();
            
            switch (opType)
            {
                case OperationType.Addition:
                    return new AdditionNode(left, right);
                case OperationType.Subtraction:
                    return new SubtractionNode(left, right);
                case OperationType.Multiplication:
                    return new MultiplicationNode(left, right);
                case OperationType.Division:
                    return new DivisionNode(left, right);
                case OperationType.Exponential:
                    return new ExponentialNode(left, right);
                default:
                    Console.WriteLine($"Failed to read operation after keyword {TokenType.Set}.");
                    return default;
            }
        }
    }
}
