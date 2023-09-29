using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Moth.Tokens;
using System.Collections;
using Moth.AST.Node;

namespace Moth.AST;

public static class TokenParser
{
    public static ScriptAST ProcessScript(ParseContext context)
    {
        if (context.Current?.Type == TokenType.Namespace)
        {
            context.MoveNext();
            string @namespace = context.Current.Value.Text.ToString(); ;
            List<string> imports = new List<string>();
            List<FuncDefNode> funcs = new List<FuncDefNode>();
            List<ClassNode> classes = new List<ClassNode>();

            while (context.Current != null)
            {
                switch (context.Current?.Type)
                {
                    case TokenType.Foreign:
                    case TokenType.Function:
                        bool isForeign = context.Current?.Type == TokenType.Foreign;
                        context.MoveNext();

                        if (context.Current?.Type == TokenType.Name)
                        {
                            string name = context.Current.Value.Text.ToString();
                            context.MoveNext();

                            if (context.Current?.Type == TokenType.OpeningParentheses)
                            {
                                context.MoveNext();
                                var @params = ProcessParameterList(context, out bool isVariadic);

                                if (context.Current?.Type == TokenType.Colon)
                                {
                                    context.MoveNext();

                                    if (context.Current?.Type == TokenType.Name)
                                    {
                                        string retTypeName = context.Current.Value.Text.ToString();
                                        bool returnsPointer = false;
                                        context.MoveNext();

                                        if (context.Current?.Type == TokenType.Asterix)
                                        {
                                            returnsPointer = true;
                                            context.MoveNext();
                                        }

                                        var retTypeRef = new TypeRefNode(retTypeName, returnsPointer);

                                        if (context.Current?.Type == TokenType.OpeningCurlyBraces)
                                        {
                                            context.MoveNext();
                                            funcs.Add(new FuncDefNode(name,PrivacyType.Public, retTypeRef, @params,
                                                ProcessBlock(context), isVariadic));
                                            break;
                                        }
                                        else
                                        {
                                            throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningCurlyBraces);
                                        }
                                    }
                                    else
                                    {
                                        throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
                                    }
                                }
                                else
                                {
                                    throw new UnexpectedTokenException(context.Current.Value, TokenType.Colon);
                                }
                            }
                            else
                            {
                                throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningParentheses);
                            }
                        }
                        else
                        {
                            throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
                        }
                    case TokenType.Public:
                        context.MoveNext();

                        if (context.Current?.Type == TokenType.Class)
                        {
                            context.MoveNext();

                            if (context.Current?.Type == TokenType.Name)
                            {
                                string className = context.Current.Value.Text.ToString();
                                context.MoveNext();
                                classes.Add(ProcessClass(context, PrivacyType.Public, className));
                            }
                        }
                        else
                        {
                            throw new UnexpectedTokenException(context.Current.Value, TokenType.Class);
                        }

                        break;
                    case TokenType.Private:
                        context.MoveNext();

                        if (context.Current?.Type == TokenType.Class)
                        {
                            context.MoveNext();

                            if (context.Current?.Type == TokenType.Name)
                            {
                                string className = context.Current.Value.Text.ToString();
                                context.MoveNext();
                                classes.Add(ProcessClass(context, PrivacyType.Private, className));
                            }
                        }
                        else
                        {
                            throw new UnexpectedTokenException(context.Current.Value, TokenType.Class);
                        }

                        break;
                    default:
                        throw new UnexpectedTokenException(context.Current.Value);
                }
            }

            return new ScriptAST(@namespace, imports, classes, funcs);
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Namespace);
        }
    }

    public static ClassNode ProcessClass(ParseContext context, PrivacyType privacy, string name)
    {
        if (context.Current?.Type == TokenType.OpeningCurlyBraces)
        {
            context.MoveNext();
            return new ClassNode(name, privacy, ProcessBlock(context, true));
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningCurlyBraces);
        }
    }

    public static ScopeNode ProcessBlock(ParseContext context, bool isClassRoot = false) //TODO: Rewrite time!
    {
        List<StatementNode> statements = new List<StatementNode>();

        while (context.Current != null)
        {
            switch (context.Current?.Type)
            {
                case TokenType.OpeningCurlyBraces:
                    if (!isClassRoot)
                    {
                        context.MoveNext();
                        statements.Add(ProcessBlock(context));
                        break;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }
                case TokenType.ClosingCurlyBraces:
                    context.MoveNext();
                    return new ScopeNode(statements);
                case TokenType.If:
                    if (!isClassRoot)
                    {
                        context.MoveNext();
                        statements.Add(ProcessIf(context));
                        break;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }
                case TokenType.Return:
                    if (!isClassRoot)
                    {
                        context.MoveNext();
                        statements.Add(new ReturnNode(ProcessExpression(context, null, TokenType.Semicolon)));
                        break;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }
                case TokenType.Public:
                case TokenType.Private:
                case TokenType.Local:
                    PrivacyType privacyType = PrivacyType.Public;
                    bool isConstant = false;
                    bool isVariadic = false;

                    if (context.Current?.Type == TokenType.Private)
                    {
                        privacyType = PrivacyType.Private;
                    }
                    else if (context.Current?.Type == TokenType.Local)
                    {
                        if (isClassRoot)
                        {
                            throw new UnexpectedTokenException(context.Current.Value);
                        }

                        privacyType = PrivacyType.Local;
                    }

                    context.MoveNext();

                    if (privacyType != PrivacyType.Local)
                    {
                        if (context.Current?.Type == TokenType.Variadic)
                        {
                            isVariadic = true;
                            context.MoveNext();
                        }
                    }

                    if (context.Current?.Type == TokenType.Constant)
                    {
                        isConstant = true;
                        context.MoveNext();
                    }

                    if (privacyType == PrivacyType.Local)
                    {
                        List<TokenType> peek3List = new List<TokenType>();

                        foreach (Token token in context.Peek(3))
                        {
                            peek3List.Add(token.Type);
                        }

                        TokenType[] peek3 = peek3List.ToArray();
                        TokenType[] comparer = new TokenType[] { TokenType.Name, TokenType.Name, TokenType.Semicolon };

                        if (peek3[0] == comparer[0] && peek3[1] == comparer[1] && peek3[2] == comparer[2])
                        {
                            string typeRef = context.Current.Value.Text.ToString();
                            context.MoveNext();
                            statements.Add(ProcessDefinition(context, privacyType, typeRef, isConstant, false, false));
                            break;
                        }
                        else
                        {
                            ExpressionNode val = ProcessExpression(context, null, TokenType.Colon);
                            statements.Add(ProcessDefinition(context, privacyType, val, isConstant, false, false));
                            break;
                        }
                    }
                    else
                    {
                        string typeRef = context.Current.Value.Text.ToString();
                        context.MoveNext();
                        statements.Add(ProcessDefinition(context, privacyType, typeRef, isConstant, false, isVariadic));
                        break;
                    }
                case TokenType.This:
                case TokenType.Name:
                    if (!isClassRoot)
                    {
                        statements.Add(ProcessExpression(context, null, TokenType.Semicolon));
                        break;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }
                case TokenType.Increment:
                case TokenType.Decrement:
                    if (!isClassRoot)
                    {
                        TokenType type = (TokenType)(context.Current?.Type);
                        RefNode refNode;
                        context.MoveNext();

                        if (context.Current?.Type == TokenType.This || context.Current?.Type == TokenType.Name)
                        {
                            refNode = ProcessAccess(context);
                        }
                        else
                        {
                            throw new UnexpectedTokenException(context.Current.Value);
                        }

                        if (type == TokenType.Decrement)
                        {
                            statements.Add(new IncrementVarNode(refNode));
                        }
                        else
                        {
                            statements.Add(new DecrementVarNode(refNode));
                        }

                        break;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }
                default:
                    throw new UnexpectedTokenException(context.Current.Value);
            }
        }

        throw new UnexpectedTokenException(context.Current.Value);
    }

    public static StatementNode ProcessDefinition(ParseContext context,
        PrivacyType privacyType, object typeRef, bool isConstant, bool isForeign, bool isVariadic)
    {
        string name;

        if (context.Current?.Type == TokenType.Name)
        {
            name = context.Current.Value.Text.ToString();
            context.MoveNext();
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
        }

        if (typeRef is string strTypeRef)
        {
            if (context.Current?.Type == TokenType.OpeningParentheses)
            {
                context.MoveNext();
                var @params = ProcessParameterList(context);

                if (!isForeign && context.Current?.Type == TokenType.OpeningCurlyBraces)
                {
                    context.MoveNext();
                    var scope = ProcessBlock(context);

                    return new FuncDefNode(name, privacyType, strTypeRef, @params, scope, isVariadic);
                }
                else if (isForeign && context.Current?.Type == TokenType.Semicolon)
                {
                    context.MoveNext();
                    return new FuncDefNode(name, privacyType, strTypeRef, @params, null, isVariadic);
                }
                else
                {
                    throw new UnexpectedTokenException(context.Current.Value);
                }
            }
            else if (context.Current?.Type == TokenType.Semicolon)
            {
                context.MoveNext();
                return new FieldNode(name, privacyType, strTypeRef, isConstant);
            }
            else
            {
                throw new UnexpectedTokenException(context.Current.Value);
            }
        }
        else if (typeRef is ExpressionNode expr)
        {
            if (context.Current?.Type == TokenType.Semicolon)
            {
                context.MoveNext();
                return new InferredLocalDefNode(name, privacyType, isConstant, expr);
            }
            else
            {
                throw new UnexpectedTokenException(context.Current.Value);
            }
        }
        else
        {
            throw new Exception("Failed to parse definition.");
        }
    }

    public static List<ParameterNode> ProcessParameterList(ParseContext context, out bool isVariadic)
    {
        List<ParameterNode> @params = new List<ParameterNode>(); //TODO: add colon checks to the types

        while (context.Current != null)
        {
            switch (context.Current?.Type)
            {
                case TokenType.ClosingParentheses:
                    context.MoveNext();
                    return @params;
                case TokenType.Comma:
                    context.MoveNext();
                    break;
                default:
                    @params.Add(ProcessParameter(context));
                    break;
            }
        }

        throw new UnexpectedTokenException(context.Current.Value);
    }

    public static ParameterNode ProcessParameter(ParseContext context)
    {
        string typeRef;

        if (context.Current?.Type == TokenType.Name)
        {
            typeRef = context.Current.Value.Text.ToString();
            context.MoveNext();
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
        }

        if (context.Current?.Type == TokenType.Name)
        {
            string name = context.Current.Value.Text.ToString();
            context.MoveNext();
            return new ParameterNode(name, typeRef);
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
        }
    }

    public static IfNode ProcessIf(ParseContext context)
    {
        var condition = ProcessExpression(context, null);

        if (context.Current?.Type != TokenType.OpeningCurlyBraces)
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningCurlyBraces);
        }

        context.MoveNext();
        var then = ProcessBlock(context);
        var @else = ProcessElse(context);
        return new IfNode(condition, then, @else);
    }

    public static ScopeNode? ProcessElse(ParseContext context)
    {
        if (context.Current?.Type == TokenType.Else)
        {
            context.MoveNext();

            if (context.Current?.Type == TokenType.If)
            {
                context.MoveNext();
                return new ScopeNode(new List<StatementNode>
                {
                    ProcessIf(context)
                });
            }
            else
            {
                context.MoveNext();
                return ProcessBlock(context);
            }
        }
        else
        {
            return null;
        }
    }

    public static List<ExpressionNode> ProcessArgs(ParseContext context)
    {
        List<ExpressionNode> args = new List<ExpressionNode>();

        while (context.Current != null)
        {
            if (context.Current?.Type == TokenType.ClosingParentheses)
            {
                context.MoveNext();
                break;
            }
            args.Add(ProcessExpression(context, null));
            
            if (context.Current?.Type == TokenType.ClosingParentheses)
            {
                context.MoveNext();
                break;
            }
            else if (context.Current?.Type != TokenType.Comma)
            {
                throw new UnexpectedTokenException(context.Current.Value, TokenType.ClosingParentheses);
            }

            context.MoveNext();
        }

        return args;
    }

    // Set lastCreatedNode to null when calling the parent, if not calling parent pass down the variable through all methods.
    public static ExpressionNode ProcessExpression(ParseContext context, ExpressionNode lastCreatedNode)
    {
        while (context.Current != null)
        {
            switch (context.Current?.Type)
            {
                case TokenType.OpeningParentheses:
                    context.MoveNext();
                    lastCreatedNode = ProcessExpression(context, lastCreatedNode);

                    if (context.Current?.Type != TokenType.ClosingParentheses)
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.ClosingParentheses);
                    }

                    context.MoveNext();
                    break;
                case TokenType.LiteralFloat:
                    lastCreatedNode = new ConstantNode(float.Parse(context.Current.Value.Text.Span));
                    context.MoveNext();
                    break;
                case TokenType.LiteralInt:
                    lastCreatedNode = new ConstantNode(int.Parse(context.Current.Value.Text.Span));
                    context.MoveNext();
                    break;
                case TokenType.LiteralString:
                    lastCreatedNode = new ConstantNode(context.Current.Value.Text.ToString());
                    context.MoveNext();
                    break;
                case TokenType.Plus:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Addition, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.Hyphen:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Subtraction, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.Asterix:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Multiplication, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.ForwardSlash:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Division, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.Exponential:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Exponential, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.Equal:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Equal, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.NotEqual:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.NotEqual, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.LargerThan:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LargerThan, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.LessThan:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LessThan, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.LargerThanOrEqual:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LargerThanOrEqual, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.LessThanOrEqual:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LessThanOrEqual, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.LogicalOr:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LogicalOr, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.LogicalXor:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LogicalXor, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.LogicalAnd:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LogicalAnd, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.LogicalNand:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LogicalNand, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.Name:
                case TokenType.This:
                    lastCreatedNode = ProcessAccess(context);
                    break;
                case TokenType.Assign:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Assignment, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                default:
                    if (lastCreatedNode == null)
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    return lastCreatedNode;
            }
        }

        throw new UnexpectedTokenException(context.Current.Value);
    }

    public static RefNode ProcessNew(ParseContext context)
    {
        string name = context.Current.Value.Text.ToString();
        context.MoveNext();

        if (context.Current?.Type != TokenType.OpeningParentheses)
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningParentheses);
        }

        context.MoveNext();
        var newRefNode = new RefNode(name); //TODO: this lacks generic class compat?
        newRefNode.Child = new MethodCallNode(name, ProcessArgs(context));
        return newRefNode;
    }

    public static RefNode ProcessAccess(ParseContext context)
    {
        RefNode newRefNode = null;

        if (context.Current?.Type == TokenType.This)
        {
            newRefNode = new ThisNode();
        }

        while (context.Current != null)
        {
            if (context.Current?.Type == TokenType.Name)
            {
                RefNode childNode = null;
                string name = context.Current.Value.Text.ToString();
                context.MoveNext();

                switch (context.Current?.Type)
                {
                    case TokenType.OpeningParentheses:
                        context.MoveNext();
                        childNode = new MethodCallNode(name, ProcessArgs(context));

                        if (newRefNode == null)
                        {
                            newRefNode = childNode;
                        }
                        else
                        {
                            newRefNode.Child = childNode;
                            newRefNode = newRefNode.Child;
                        }

                        break;
                    case TokenType.OpeningSquareBrackets:
                        context.MoveNext();
                        childNode = new IndexAccessNode(ProcessExpression(context, null));

                        if (context.Current?.Type != TokenType.ClosingSquareBrackets)
                        {
                            throw new UnexpectedTokenException(context.Current.Value, TokenType.ClosingSquareBrackets);
                        }

                        context.MoveNext();

                        if (newRefNode == null)
                        {
                            newRefNode = childNode;
                        }
                        else
                        {
                            newRefNode.Child = childNode;
                            newRefNode = newRefNode.Child;
                        }

                        break;
                    default:
                        childNode = new RefNode(name);

                        if (newRefNode == null)
                        {
                            newRefNode = childNode;
                        }
                        else
                        {
                            newRefNode.Child = childNode;
                            newRefNode = newRefNode.Child;
                        }

                        break;
                }

                if (context.Current?.Type == TokenType.Period)
                {
                    context.MoveNext();
                }
                else
                {
                    return newRefNode;
                }
            }
            else
            {
                return newRefNode;
            }
        }

        throw new UnexpectedTokenException(context.Current.Value);
    }

    public static ExpressionNode ProcessBinaryOp(ParseContext context, OperationType opType, ExpressionNode left)
    {
        var right = ProcessExpression(context, left);

        switch (opType)
        {
            case OperationType.Assignment:
                {
                    if (left is BinaryOperationNode bin)
                    {
                        if (GetOpPriority(bin.Type) < GetOpPriority(opType))
                        {
                            bin.Right = new BinaryOperationNode(bin.Right, right, OperationType.Assignment);
                            return bin.Right;
                        }
                    }
                }

                return new BinaryOperationNode(left, right, OperationType.Assignment);
            case OperationType.Addition:
                {
                    if (left is BinaryOperationNode bin)
                    {
                        if (GetOpPriority(bin.Type) < GetOpPriority(opType))
                        {
                            bin.Right = new BinaryOperationNode(bin.Right, right, OperationType.Addition);
                            return bin.Right;
                        }
                    }
                }

                return new BinaryOperationNode(left, right, OperationType.Addition);
            case OperationType.Subtraction:
                {
                    if (left is BinaryOperationNode bin)
                    {
                        if (GetOpPriority(bin.Type) < GetOpPriority(opType))
                        {
                            bin.Right = new BinaryOperationNode(bin.Right, right, OperationType.Subtraction);
                            return bin.Right;
                        }
                    }
                }

                return new BinaryOperationNode(left, right, OperationType.Subtraction);
            case OperationType.Modulo:
                {
                    if (left is BinaryOperationNode bin)
                    {
                        if (GetOpPriority(bin.Type) < GetOpPriority(opType))
                        {
                            bin.Right = new BinaryOperationNode(bin.Right, right, OperationType.Modulo);
                            return bin.Right;
                        }
                    }
                }

                return new BinaryOperationNode(left, right, OperationType.Modulo);
            case OperationType.Multiplication:
                {
                    if (left is BinaryOperationNode bin)
                    {
                        if (GetOpPriority(bin.Type) < GetOpPriority(opType))
                        {
                            bin.Right = new BinaryOperationNode(bin.Right, right, OperationType.Multiplication);
                            return bin.Right;
                        }
                    }
                }

                return new BinaryOperationNode(left, right, OperationType.Multiplication);
            case OperationType.Division:
                {
                    if (left is BinaryOperationNode bin)
                    {
                        if (GetOpPriority(bin.Type) < GetOpPriority(opType))
                        {
                            bin.Right = new BinaryOperationNode(bin.Right, right, OperationType.Division);
                            return bin.Right;
                        }
                    }
                }

                return new BinaryOperationNode(left, right, OperationType.Division);
            case OperationType.Exponential:
                {
                    if (left is BinaryOperationNode bin)
                    {
                        if (GetOpPriority(bin.Type) < GetOpPriority(opType))
                        {
                            bin.Right = new BinaryOperationNode(bin.Right, right, OperationType.Exponential);
                            return bin.Right;
                        }
                    }
                }

                return new BinaryOperationNode(left, right, OperationType.Exponential);
            case OperationType.Equal:
                {
                    if (left is BinaryOperationNode bin)
                    {
                        if (GetOpPriority(bin.Type) < GetOpPriority(opType))
                        {
                            bin.Right = new BinaryOperationNode(bin.Right, right, OperationType.Equal);
                            return bin.Right;
                        }
                    }
                }

                return new BinaryOperationNode(left, right, OperationType.Equal);
            case OperationType.NotEqual:
                {
                    if (left is BinaryOperationNode bin)
                    {
                        if (GetOpPriority(bin.Type) < GetOpPriority(opType))
                        {
                            bin.Right = new BinaryOperationNode(bin.Right, right, OperationType.NotEqual);
                            return bin.Right;
                        }
                    }
                }

                return new BinaryOperationNode(left, right, OperationType.NotEqual);
            case OperationType.LessThanOrEqual:
                {
                    if (left is BinaryOperationNode bin)
                    {
                        if (GetOpPriority(bin.Type) < GetOpPriority(opType))
                        {
                            bin.Right = new BinaryOperationNode(bin.Right, right, OperationType.LessThanOrEqual);
                            return bin.Right;
                        }
                    }
                }

                return new BinaryOperationNode(left, right, OperationType.LessThanOrEqual);
            case OperationType.LargerThanOrEqual:
                {
                    if (left is BinaryOperationNode bin)
                    {
                        if (GetOpPriority(bin.Type) < GetOpPriority(opType))
                        {
                            bin.Right = new BinaryOperationNode(bin.Right, right, OperationType.LargerThanOrEqual);
                            return bin.Right;
                        }
                    }
                }

                return new BinaryOperationNode(left, right, OperationType.LargerThanOrEqual);
            case OperationType.Or:
                {
                    if (left is BinaryOperationNode bin)
                    {
                        if (GetOpPriority(bin.Type) < GetOpPriority(opType))
                        {
                            bin.Right = new BinaryOperationNode(bin.Right, right, OperationType.Or);
                            return bin.Right;
                        }
                    }
                }

                return new BinaryOperationNode(left, right, OperationType.Or);
            case OperationType.LessThan:
                {
                    if (left is BinaryOperationNode bin)
                    {
                        if (GetOpPriority(bin.Type) < GetOpPriority(opType))
                        {
                            bin.Right = new BinaryOperationNode(bin.Right, right, OperationType.LessThan);
                            return bin.Right;
                        }
                    }
                }

                return new BinaryOperationNode(left, right, OperationType.LessThan);
            case OperationType.LargerThan:
                {
                    if (left is BinaryOperationNode bin)
                    {
                        if (GetOpPriority(bin.Type) < GetOpPriority(opType))
                        {
                            bin.Right = new BinaryOperationNode(bin.Right, right, OperationType.LargerThan);
                            return bin.Right;
                        }
                    }
                }

                return new BinaryOperationNode(left, right, OperationType.LargerThan);
            case OperationType.And:
                {
                    if (left is BinaryOperationNode bin)
                    {
                        if (GetOpPriority(bin.Type) < GetOpPriority(opType))
                        {
                            bin.Right = new BinaryOperationNode(bin.Right, right, OperationType.And);
                            return bin.Right;
                        }
                    }
                }

                return new BinaryOperationNode(left, right, OperationType.And);
            case OperationType.LogicalNand:
                {
                    if (left is BinaryOperationNode bin)
                    {
                        if (GetOpPriority(bin.Type) < GetOpPriority(opType))
                        {
                            bin.Right = new BinaryOperationNode(bin.Right, right, OperationType.LogicalNand);
                            return bin.Right;
                        }
                    }
                }

                return new BinaryOperationNode(left, right, OperationType.LogicalNand);
            default:
                throw new UnexpectedTokenException(context.Current.Value);
        }
    }

    private static int GetOpPriority(OperationType operationType)
    {
        switch (operationType)
        {
            case OperationType.Exponential:
                return 4;
            case OperationType.Modulo:
            case OperationType.Multiplication:
            case OperationType.Division:
                return 3;
            case OperationType.Addition:
            case OperationType.Subtraction:
                return 2;
            case OperationType.Equal:
            case OperationType.NotEqual:
            case OperationType.LessThanOrEqual:
            case OperationType.LargerThanOrEqual:
            case OperationType.LogicalOr:
            case OperationType.LogicalXor:
            case OperationType.LessThan:
            case OperationType.LargerThan:
            case OperationType.LogicalAnd:
            case OperationType.LogicalNand:
                return 1;
            case OperationType.Assignment:
                return 0;
            default:
                return 0;
        }
    }
}
