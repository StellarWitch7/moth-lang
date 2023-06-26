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

    public ScriptAST ProcessScript()
    {
        AssignNamespaceNode assignNamespaceNode = null;
        List<ImportNode> imports = new List<ImportNode>();
        List<ClassNode> classes = new List<ClassNode>();

        if (_context.Current?.Type == TokenType.NamespaceTag)
        {
            _context.MoveNext();
            ProcessNamespaceAssignment();
        }
        else
        {
            throw new UnexpectedTokenException(_context.Current.Value, TokenType.NamespaceTag);
        }

        while (_context.Current != null)
        {
            switch (_context.Current?.Type)
            {
                case TokenType.Import:
                    _context.MoveNext();
                    imports.Add(ProcessImport());
                    break;
                case TokenType.Public:
                    _context.MoveNext();

                    if (_context.Current?.Type == TokenType.Class)
                    {
                        _context.MoveNext();

                        if (_context.Current?.Type == TokenType.Name)
                        {
                            string className = _context.Current.Value.Text.ToString();
                            _context.MoveNext();
                            classes.Add(ProcessClass(PrivacyType.Public, className));
                        }
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value, TokenType.Class);
                    }

                    break;
                case TokenType.Private:
                    _context.MoveNext();

                    if (_context.Current?.Type == TokenType.Class)
                    {
                        _context.MoveNext();
                        
                        if (_context.Current?.Type == TokenType.Name)
                        {
                            string className = _context.Current.Value.Text.ToString();
                            _context.MoveNext();
                            classes.Add(ProcessClass(PrivacyType.Private, className));
                        }
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value, TokenType.Class);
                    }

                    break;
                default:
                    throw new UnexpectedTokenException(_context.Current.Value);
            }
        }

        return new ScriptAST(assignNamespaceNode, imports, classes);
    }

    private NamespaceNode ProcessNamespace()
    {
        List<string> @namespace = new List<string>();

        while (_context.Current != null)
        {
            switch (_context.Current?.Type)
            {
                case TokenType.Semicolon:
                    _context.MoveNext();
                    return new NamespaceNode(@namespace);
                case TokenType.Name:
                    @namespace.Add(_context.Current.Value.Text.ToString());
                    _context.MoveNext();
                    break;
                case TokenType.Period:
                    @namespace.Add(_context.Current.Value.Text.ToString());
                    _context.MoveNext();
                    break;
                default:
                    throw new UnexpectedTokenException(_context.Current.Value);
            }
        }

        throw new UnexpectedTokenException(_context.Current.Value);
    }

    private AssignNamespaceNode ProcessNamespaceAssignment()
    {
        return new AssignNamespaceNode(ProcessNamespace());
    }

    private ImportNode ProcessImport()
    {
        if (_context.Current?.Type == TokenType.NamespaceTag)
        {
            _context.MoveNext();
            return new ImportNode(ProcessNamespace());
        }
        else
        {
            throw new UnexpectedTokenException(_context.Current.Value, TokenType.NamespaceTag);
        }
    }

    private ClassNode ProcessClass(PrivacyType privacy, string name)
    {
        if (_context.Current?.Type == TokenType.OpeningCurlyBraces)
        {
            _context.MoveNext();
            return new ClassNode(name, privacy, ProcessStatementList(true));
        }
        else
        {
            throw new UnexpectedTokenException(_context.Current.Value, TokenType.OpeningCurlyBraces);
        }
    }

    private StatementListNode? ProcessStatementList(bool isClassRoot = false)
    {
        List<StatementNode> statements = new List<StatementNode>();

        while (_context.Current != null)
        {
            switch (_context.Current?.Type)
            {
                case TokenType.OpeningCurlyBraces:
                    if (!isClassRoot)
                    {
                        _context.MoveNext();
                        statements.Add(ProcessStatementList());
                        break;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                case TokenType.ClosingCurlyBraces:
                    _context.MoveNext();
                    return new StatementListNode(statements);
                case TokenType.Set:
                    if (!isClassRoot)
                    {
                        _context.MoveNext();
                        statements.Add(ProcessAssignment());
                        break;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                case TokenType.Call:
                    if (!isClassRoot)
                    {
                        _context.MoveNext();
                        statements.Add(ProcessCall());
                        break;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                case TokenType.If:
                    if (!isClassRoot)
                    {
                        _context.MoveNext();
                        statements.Add(ProcessIf());
                        break;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value);
                    }
                case TokenType.Public:
                    if (isClassRoot)
                    {
                        bool isConstant = false;
                        _context.MoveNext();

                        if (_context.Current?.Type == TokenType.Constant)
                        {
                            isConstant = true;
                            _context.MoveNext();
                        }

                        switch (_context.Current?.Type)
                        {
                            case TokenType.Bool:
                                _context.MoveNext();
                                statements.Add(ProcessDefinition(PrivacyType.Public, isConstant,
                                    DefinitionType.Bool));
                                break;
                            case TokenType.String:
                                _context.MoveNext();
                                statements.Add(ProcessDefinition(PrivacyType.Public, isConstant,
                                    DefinitionType.String));
                                break;
                            case TokenType.Int32:
                                _context.MoveNext();
                                statements.Add(ProcessDefinition(PrivacyType.Public, isConstant,
                                    DefinitionType.Int32));
                                break;
                            case TokenType.Float32:
                                _context.MoveNext();
                                statements.Add(ProcessDefinition(PrivacyType.Public, isConstant,
                                    DefinitionType.Float32));
                                break;
                            case TokenType.Void:
                                if (isConstant)
                                {
                                    throw new UnexpectedTokenException(_context.Current.Value);
                                }

                                _context.MoveNext();
                                statements.Add(ProcessDefinition(PrivacyType.Public, false,
                                    DefinitionType.Void));
                                break;
                            case TokenType.Name:
                                var classRef = new ClassRefNode(_context.Current.Value.Text.ToString());
                                _context.MoveNext();
                                statements.Add(ProcessDefinition(PrivacyType.Public, isConstant,
                                    DefinitionType.ClassObject, classRef));
                                break;
                            default:
                                throw new UnexpectedTokenException(_context.Current.Value);
                        }

                        break;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value, TokenType.Var);
                    }
                case TokenType.Private:
                    if (isClassRoot)
                    {
                        bool isConstant = false;
                        _context.MoveNext();

                        if (_context.Current?.Type == TokenType.Constant)
                        {
                            isConstant = true;
                            _context.MoveNext();
                        }

                        switch (_context.Current?.Type)
                        {
                            case TokenType.Bool:
                                _context.MoveNext();
                                statements.Add(ProcessDefinition(PrivacyType.Private, isConstant,
                                    DefinitionType.Bool));
                                break;
                            case TokenType.String:
                                _context.MoveNext();
                                statements.Add(ProcessDefinition(PrivacyType.Private, isConstant,
                                    DefinitionType.String));
                                break;
                            case TokenType.Int32:
                                _context.MoveNext();
                                statements.Add(ProcessDefinition(PrivacyType.Private, isConstant,
                                    DefinitionType.Int32));
                                break;
                            case TokenType.Float32:
                                _context.MoveNext();
                                statements.Add(ProcessDefinition(PrivacyType.Private, isConstant,
                                    DefinitionType.Float32));
                                break;
                            case TokenType.Void:
                                if (isConstant)
                                {
                                    throw new UnexpectedTokenException(_context.Current.Value);
                                }

                                _context.MoveNext();
                                statements.Add(ProcessDefinition(PrivacyType.Private, false,
                                    DefinitionType.Void));
                                break;
                            case TokenType.Name:
                                var classRef = new ClassRefNode(_context.Current.Value.Text.ToString());
                                _context.MoveNext();
                                statements.Add(ProcessDefinition(PrivacyType.Private, isConstant,
                                    DefinitionType.ClassObject, classRef));
                                break;
                            default:
                                throw new UnexpectedTokenException(_context.Current.Value);
                        }

                        break;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(_context.Current.Value, TokenType.Var);
                    }
                default:
                    _context.MoveNext();
                    break;
            }
        }

        throw new UnexpectedTokenException(_context.Current.Value);
    }

    private StatementNode ProcessDefinition(PrivacyType privacyType, bool isConstant,
        DefinitionType fieldType, ClassRefNode? classRef = null)
    {
        string name;

        if (_context.Current?.Type == TokenType.Name)
        {
            name = _context.Current.Value.Text.ToString();
            _context.MoveNext();
        }
        else
        {
            throw new UnexpectedTokenException(_context.Current.Value, TokenType.Name);
        }

        if (_context.Current?.Type == TokenType.OpeningParentheses)
        {
            _context.MoveNext();
            var @params = ProcessParameterList();

            if (_context.Current?.Type == TokenType.OpeningCurlyBraces)
            {
                _context.MoveNext();
                var statements = ProcessStatementList();
                return new MethodDefNode(name, @params, statements);
            }
            else
            {
                throw new UnexpectedTokenException(_context.Current.Value);
            }
        }
        else if (_context.Current?.Type == TokenType.Semicolon)
        {
            _context.MoveNext();
            return new FieldNode(name, privacyType, fieldType, isConstant);
        }
        else
        {
            throw new UnexpectedTokenException(_context.Current.Value);
        }
    }

    private ParameterListNode ProcessParameterList()
    {
        List<ParameterNode> @params = new List<ParameterNode>();

        while (_context.Current != null)
        {
            switch (_context.Current?.Type)
            {
                case TokenType.ClosingParentheses:
                    _context.MoveNext();
                    return new ParameterListNode(@params);
                case TokenType.Comma:
                    _context.MoveNext();
                    break;
                default:
                    @params.Add(ProcessParameter());
                    break;
            }
        }

        throw new UnexpectedTokenException(_context.Current.Value);
    }

    private ParameterNode ProcessParameter()
    {
        switch (_context.Current?.Type)
        {
            case TokenType.Bool:
                _context.MoveNext();

                if (_context.Current?.Type == TokenType.Name)
                {
                    string name = _context.Current.Value.Text.ToString();
                    return new ParameterNode(DefinitionType.Bool, name, null);
                }
                else
                {
                    throw new UnexpectedTokenException(_context.Current.Value, TokenType.Name);
                }
            case TokenType.String:
                _context.MoveNext();

                if (_context.Current?.Type == TokenType.Name)
                {
                    string name = _context.Current.Value.Text.ToString();
                    return new ParameterNode(DefinitionType.String, name, null);
                }
                else
                {
                    throw new UnexpectedTokenException(_context.Current.Value, TokenType.Name);
                }
            case TokenType.Int32:
                _context.MoveNext();

                if (_context.Current?.Type == TokenType.Name)
                {
                    string name = _context.Current.Value.Text.ToString();
                    return new ParameterNode(DefinitionType.Int32, name, null);
                }
                else
                {
                    throw new UnexpectedTokenException(_context.Current.Value, TokenType.Name);
                }
            case TokenType.Float32:
                _context.MoveNext();

                if (_context.Current?.Type == TokenType.Name)
                {
                    string name = _context.Current.Value.Text.ToString();
                    return new ParameterNode(DefinitionType.Float32, name, null);
                }
                else
                {
                    throw new UnexpectedTokenException(_context.Current.Value, TokenType.Name);
                }
            case TokenType.Matrix:
                _context.MoveNext();

                if (_context.Current?.Type == TokenType.Name)
                {
                    string name = _context.Current.Value.Text.ToString();
                    return new ParameterNode(DefinitionType.Matrix, name, null);
                }
                else
                {
                    throw new UnexpectedTokenException(_context.Current.Value, TokenType.Name);
                }
            case TokenType.Name:
                var typeName = new ClassRefNode(_context.Current.Value.Text.ToString());
                _context.MoveNext();

                if (_context.Current?.Type == TokenType.Name)
                {
                    string name = _context.Current.Value.Text.ToString();
                    return new ParameterNode(DefinitionType.ClassObject, name, typeName);
                }
                else
                {
                    throw new UnexpectedTokenException(_context.Current.Value, TokenType.Name);
                }
            default:
                throw new UnexpectedTokenException(_context.Current.Value);
        }
    }

    private IfNode ProcessIf()
    {
        var condition = ProcessExpression();
        _context.MoveNext();
        var then = ProcessStatementList();
        var @else = ProcessElse();
        return new IfNode(condition, then, @else);
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

    private MethodCallNode ProcessMethod()
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
        return new MethodCallNode(methodName, originClass, args);
    }

    private AssignmentNode ProcessAssignment()
    {
        var newVarNode = new VariableRefNode(_context.Current?.Text.ToString() ?? string.Empty);
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
                case TokenType.Name:
                    newExprNode = new VariableRefNode(_context.Current.Value.Text.ToString());
                    _context.MoveNext();
                    exprFound = true;
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
