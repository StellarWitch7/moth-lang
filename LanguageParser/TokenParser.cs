using LanguageParser.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using LanguageParser.Tokens;

namespace LanguageParser;

internal class TokenParser
{
    private readonly ParseContext _context;

    public TokenParser(List<Token> tokens)
    {
        _context = new ParseContext(tokens);
    }

    public StatementListNode? ProcessStatementList()
    {
        List<StatementNode> statements = new List<StatementNode>();

        while (_context.Current != null)
        {
            switch (_context.Current?.Type)
            {
                case TokenType.OpeningCurlyBraces:
                    _context.MoveNext();
                    statements.Add(ProcessStatementList());
                    break;
                case TokenType.ClosingCurlyBraces:
                    _context.MoveNext();
                    return new StatementListNode(statements);
                case TokenType.Set:
                    _context.MoveNext();
                    statements.Add(ProcessAssignment());
                    break;
                case TokenType.Call:
                    _context.MoveNext();
                    statements.Add(ProcessCall());
                    break;
                case TokenType.If:
                    _context.MoveNext();
                    statements.Add(ProcessIf());
                    break;
                default:
                    _context.MoveNext();
                    break;
            }
        }

        if (statements.Count <= 0)
        {
            return default;
        }
        else
        {
            return new StatementListNode(statements);
        }
    }

    private IfNode ProcessIf()
    {
        var condition = ProcessExpression();
        _context.MoveNext();
        var then = ProcessStatementList();
        var @else = ProcessElse();
        var @continue = ProcessStatementList();
        return new IfNode(condition, then, @else, @continue);
    }

    private StatementListNode? ProcessElse()
    {
        if (_context.Current?.Type == TokenType.Else)
        {
            _context.MoveNext();
            
            if (_context.Current?.Type == TokenType.If)
            {
                _context.MoveNext();
                return new StatementListNode(new List<StatementNode>
                {
                    ProcessIf()
                });
            }
            else
            {
                _context.MoveNext();
                return ProcessStatementList();
            }
        }
        else
        {
            return null;
        }
    }

    private CallNode ProcessCall()
    {
        return new CallNode(ProcessMethod());
    }

    private MethodNode ProcessMethod()
    {
        string originClass = _context.Current?.Text.ToString() ?? string.Empty;
        _context.MoveAmount(2);
        string methodName = _context.Current?.Text.ToString() ?? string.Empty;
        _context.MoveAmount(2);
        var args = new List<ExpressionNode>();

        while (_context.Current != null && _context.Current?.Type != TokenType.ClosingParentheses) {
            args.Add(ProcessExpression());
        }

        _context.MoveNext();
        return new MethodNode(methodName, originClass, args);
    }

    private AssignmentNode ProcessAssignment()
    {
        var newVarNode = new VariableNode(_context.Current?.Text.ToString() ?? string.Empty);
        _context.MoveAmount(2);
        var newExprNode = ProcessExpression();
        return new AssignmentNode(newVarNode, newExprNode);
    }

    private ExpressionNode? ProcessExpression()
    {
        ExpressionNode? newExprNode = null;
        bool exprFound = false;

        while (_context.Current != null && _context.Current.Value.Type != TokenType.ClosingParentheses)
        {
            switch (_context.Current.Value.Type)
            {
                case TokenType.OpeningCurlyBraces:
                    if (!exprFound)
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }

                    return newExprNode;
                case TokenType.Semicolon:
                    _context.MoveNext();

                    if (!exprFound)
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }

                    return newExprNode;
                case TokenType.Comma:
                    _context.MoveNext();

                    if (!exprFound)
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }

                    return newExprNode;
                case TokenType.Float32:
                    newExprNode = new ConstantNode(float.Parse(_context.Current.Value.Text.Span));
                    _context.MoveNext();
                    exprFound = true;
                    break;
                case TokenType.Int32:
                    newExprNode = new ConstantNode(BigInteger.Parse(_context.Current.Value.Text.Span));
                    _context.MoveNext();
                    exprFound = true;
                    break;
                case TokenType.Addition:
                    if (newExprNode != null)
                    {
                        _context.MoveNext();
                        newExprNode = ProcessBinaryOp(OperationType.Addition, newExprNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                    break;
                case TokenType.Subtraction:
                    if (newExprNode != null)
                    {
                        _context.MoveNext();
                        newExprNode = ProcessBinaryOp(OperationType.Subtraction, newExprNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                    break;
                case TokenType.Multiplication:
                    if (newExprNode != null)
                    {
                        _context.MoveNext();
                        newExprNode = ProcessBinaryOp(OperationType.Multiplication, newExprNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                    break;
                case TokenType.Division:
                    if (newExprNode != null)
                    {
                        _context.MoveNext();
                        newExprNode = ProcessBinaryOp(OperationType.Division, newExprNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                    break;
                case TokenType.Exponential:
                    if (newExprNode != null)
                    {
                        _context.MoveNext();
                        newExprNode = ProcessBinaryOp(OperationType.Exponential, newExprNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                    break;
                case TokenType.Equal:
                    if (newExprNode != null)
                    {
                        _context.MoveNext();
                        newExprNode = ProcessBinaryOp(OperationType.Equal, newExprNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                    break;
                case TokenType.NotEqual:
                    if (newExprNode != null)
                    {
                        _context.MoveNext();
                        newExprNode = ProcessBinaryOp(OperationType.NotEqual, newExprNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                    break;
                case TokenType.LargerThan:
                    if (newExprNode != null)
                    {
                        _context.MoveNext();
                        newExprNode = ProcessBinaryOp(OperationType.LargerThan, newExprNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                    break;
                case TokenType.LessThan:
                    if (newExprNode != null)
                    {
                        _context.MoveNext();
                        newExprNode = ProcessBinaryOp(OperationType.LessThan, newExprNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                    break;
                case TokenType.LargerThanOrEqual:
                    if (newExprNode != null)
                    {
                        _context.MoveNext();
                        newExprNode = ProcessBinaryOp(OperationType.LargerThanOrEqual, newExprNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                    break;
                case TokenType.LessThanOrEqual:
                    if (newExprNode != null)
                    {
                        _context.MoveNext();
                        newExprNode = ProcessBinaryOp(OperationType.LessThanOrEqual, newExprNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                    break;
                case TokenType.Or:
                    if (newExprNode != null)
                    {
                        _context.MoveNext();
                        newExprNode = ProcessBinaryOp(OperationType.Or, newExprNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                    break;
                case TokenType.And:
                    if (newExprNode != null)
                    {
                        _context.MoveNext();
                        newExprNode = ProcessBinaryOp(OperationType.And, newExprNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                    break;
                case TokenType.NotAnd:
                    if (newExprNode != null)
                    {
                        _context.MoveNext();
                        newExprNode = ProcessBinaryOp(OperationType.NotAnd, newExprNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                    break;
                default:
                    throw new UnexpectedTokenException(_context.Current.Value);
            }
        }

        if (!exprFound)
        {
            throw new UnexpectedTokenException(_context.Current.Value);
        }

        return newExprNode;
    }

    private ExpressionNode? ProcessBinaryOp(OperationType opType, ExpressionNode left)
    {
        var right = ProcessExpression();
        
        switch (opType)
        {
            case OperationType.Addition:
                return new BinaryOperationNode(left, right, OperationType.Addition);
            case OperationType.Subtraction:
                return new BinaryOperationNode(left, right, OperationType.Subtraction);
            case OperationType.Multiplication:
                return new BinaryOperationNode(left, right, OperationType.Multiplication);
            case OperationType.Division:
                return new BinaryOperationNode(left, right, OperationType.Division);
            case OperationType.Exponential:
                return new BinaryOperationNode(left, right, OperationType.Exponential);
            case OperationType.Equal:
                return new BinaryOperationNode(left, right, OperationType.Equal);
            case OperationType.NotEqual:
                return new BinaryOperationNode(left, right, OperationType.NotEqual);
            case OperationType.LessThanOrEqual:
                return new BinaryOperationNode(left, right, OperationType.LessThanOrEqual);
            case OperationType.LargerThanOrEqual:
                return new BinaryOperationNode(left, right, OperationType.LargerThanOrEqual);
            case OperationType.Or:
                return new BinaryOperationNode(left, right, OperationType.Or);
            case OperationType.LessThan:
                return new BinaryOperationNode(left, right, OperationType.LessThan);
            case OperationType.LargerThan:
                return new BinaryOperationNode(left, right, OperationType.LargerThan);
            case OperationType.And:
                return new BinaryOperationNode(left, right, OperationType.And);
            case OperationType.NotAnd:
                return new BinaryOperationNode(left, right, OperationType.NotAnd);
            default:
                throw new UnexpectedTokenException(_context.Current.Value);
        }
    }
}
