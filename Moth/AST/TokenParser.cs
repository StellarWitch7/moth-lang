using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Moth.Tokens;
using System.Collections;
using Moth.AST.Node;
using System.Diagnostics.Metrics;
using System.Xml.Linq;

namespace Moth.AST;

public static class TokenParser
{
    public static ScriptAST ProcessScript(ParseContext context)
    {
        string @namespace;
        List<string> imports = new List<string>();
        List<FuncDefNode> funcs = new List<FuncDefNode>();
        List<FieldDefNode> consts = new List<FieldDefNode>();
        List<ClassNode> classes = new List<ClassNode>();

        if (context.Current?.Type == TokenType.Namespace)
        {
            context.MoveNext();
            @namespace = ProcessNamespace(context);
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Namespace);
        }

        List<AttributeNode> attributes = new List<AttributeNode>();

        while (context.Current != null)
        {
            switch (context.Current?.Type)
            {
                case TokenType.AttributeMarker:
                    attributes.Add(ProcessAttribute(context));
                    break;
                case TokenType.Foreign:
                //case TokenType.Constant:
                case TokenType.Function:
                    var result = ProcessDefinition(context, attributes);
                    attributes = new List<AttributeNode>();

                    if (result is FuncDefNode func)
                    {
                        funcs.Add(func);
                    }
                    else if (result is FieldDefNode @const)
                    {
                        consts.Add(@const);
                    }
                    else
                    {
                        throw new Exception("Result of foreign/func was not a function.");
                    }

                    break;
                case TokenType.Public:
                case TokenType.Private:
                    PrivacyType privacyType = PrivacyType.Public;

                    if (context.Current?.Type == TokenType.Private)
                    {
                        privacyType = PrivacyType.Private;
                    }

                    context.MoveNext();

                    if (context.Current?.Type == TokenType.Class)
                    {
                        context.MoveNext();

                        if (context.Current?.Type == TokenType.Name)
                        {
                            string className = context.Current.Value.Text.ToString();
                            context.MoveNext();
                            classes.Add(ProcessClass(context, privacyType, className));
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

        return new ScriptAST(@namespace, imports, classes, funcs, consts);
    }

    public static string ProcessNamespace(ParseContext context)
    {
        var builder = new StringBuilder();

        while (context.Current != null)
        {
            switch (context.Current?.Type)
            {
                case TokenType.Period:
                    builder.Append('.');
                    context.MoveNext();
                    break;
                case TokenType.Name:
                    builder.Append(context.Current.Value.Text.ToString());
                    context.MoveNext();
                    break;
                case TokenType.Semicolon:
                    context.MoveNext();
                    return builder.ToString();
                default:
                    throw new UnexpectedTokenException(context.Current.Value);
            }
        }

        throw new UnexpectedTokenException(context.Current.Value);
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

        if (isClassRoot)
        {
            List<AttributeNode> attributes = new List<AttributeNode>();

            while (context.Current != null)
            {
                switch (context.Current?.Type)
                {
                    case TokenType.ClosingCurlyBraces:
                        context.MoveNext();
                        return new ScopeNode(statements);
                    case TokenType.Static:
                    case TokenType.Public:
                    case TokenType.Private:
                        MemberDefNode newDef = (MemberDefNode)ProcessDefinition(context, attributes);
                        attributes = new List<AttributeNode>();
                        statements.Add(newDef);
                        break;
                    case TokenType.AttributeMarker:
                        attributes.Add(ProcessAttribute(context));
                        break;
                    default:
                        throw new UnexpectedTokenException(context.Current.Value);
                }
            }
        }
        else
        {
            while (context.Current != null)
            {
                switch (context.Current?.Type)
                {
                    case TokenType.ClosingCurlyBraces:
                        context.MoveNext();
                        return new ScopeNode(statements);
                    case TokenType.OpeningCurlyBraces:
                        context.MoveNext();
                        statements.Add(ProcessBlock(context));
                        break;
                    case TokenType.If:
                        context.MoveNext();
                        statements.Add(ProcessIf(context));
                        break;
                    case TokenType.While:
                        context.MoveNext();
                        statements.Add(ProcessWhile(context));
                        break;
                    case TokenType.Return:
                        context.MoveNext();
                        statements.Add(new ReturnNode(ProcessExpression(context, null, true)));

                        if (context.Current?.Type != TokenType.Semicolon)
                        {
                            throw new UnexpectedTokenException(context.Current.Value, TokenType.Semicolon);
                        }
                        else
                        {
                            context.MoveNext();
                            break;
                        }
                    case TokenType.Local:
                        statements.Add(ProcessDefinition(context));
                        break;
                    case TokenType.TypeRef:
                    case TokenType.This:
                    case TokenType.Name:
                        statements.Add(ProcessExpression(context, null));

                        if (context.Current?.Type == TokenType.Semicolon) //TODO: swap
                        {
                            context.MoveNext();
                            break;
                        }
                        else
                        {
                            throw new UnexpectedTokenException(context.Current.Value, TokenType.Semicolon);
                        }
                    case TokenType.Increment:
                    case TokenType.Decrement:
                        statements.Add(ProcessIncrementDecrement(context));
                        break;
                    default:
                        throw new UnexpectedTokenException(context.Current.Value);
                }
            }
        }

        throw new UnexpectedTokenException(context.Current.Value);
    }

    private static ExpressionNode ProcessIncrementDecrement(ParseContext context)
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
            return new IncrementVarNode(refNode);
        }
        else
        {
            return new DecrementVarNode(refNode);
        }
    }

    public static StatementNode ProcessWhile(ParseContext context)
    {
        var condition = ProcessExpression(context, null);

        if (context.Current?.Type != TokenType.OpeningCurlyBraces)
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningCurlyBraces);
        }

        context.MoveNext();
        var then = ProcessBlock(context);
        return new WhileNode(condition, then);
    }

    public static AttributeNode ProcessAttribute(ParseContext context)
    {
        if (context.Current?.Type == TokenType.AttributeMarker)
        {
            context.MoveNext();

            if (context.Current?.Type == TokenType.Name)
            {
                string name = context.Current.Value.Text.ToString();
                context.MoveNext();

                if (context.Current?.Type == TokenType.OpeningParentheses)
                {
                    context.MoveNext();
                    var args = ProcessArgs(context);

                    return new AttributeNode(name, args);
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
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.AttributeMarker);
        }

        throw new NotImplementedException(); //TODO: attributes!
    }

    public static StatementNode ProcessDefinition(ParseContext context, List<AttributeNode> attributes = null)
    {
        PrivacyType privacy;

        if (context.Current?.Type == TokenType.Foreign)
        {
            privacy = PrivacyType.Foreign;
        }
        else if (context.Current?.Type == TokenType.Function)
        {
            privacy = PrivacyType.Global;
        }
        else if (context.Current?.Type == TokenType.Static)
        {
            privacy = PrivacyType.Static;
        }
        else if (context.Current?.Type == TokenType.Public)
        {
            privacy = PrivacyType.Public;
        }
        else if (context.Current?.Type == TokenType.Private)
        {
            privacy = PrivacyType.Private;
        }
        else if (context.Current?.Type == TokenType.Local && attributes == null)
        {
            privacy = PrivacyType.Local;
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value);
        }

        context.MoveNext();

        if (context.Current?.Type == TokenType.Name)
        {
            string name = context.Current.Value.Text.ToString();
            context.MoveNext();

            if (context.Current?.Type == TokenType.OpeningParentheses)
            {
                context.MoveNext();
                var @params = ProcessParameterList(context, out bool isVariadic);
                var retTypeRef = ProcessTypeRef(context);

                if (privacy != PrivacyType.Foreign && context.Current?.Type == TokenType.OpeningCurlyBraces)
                {
                    context.MoveNext();
                    return new FuncDefNode(name, privacy, retTypeRef, @params, ProcessBlock(context), isVariadic, attributes);
                }
                else if (privacy == PrivacyType.Foreign && context.Current?.Type == TokenType.Semicolon)
                {
                    context.MoveNext();
                    return new FuncDefNode(name, privacy, retTypeRef, @params, null, isVariadic, attributes);
                }
                else
                {
                    throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningCurlyBraces);
                }
            }
            else if (context.Current?.Type == TokenType.TypeRef)
            {
                var typeRef = ProcessTypeRef(context);

                if (context.Current?.Type == TokenType.Semicolon)
                {
                    context.MoveNext();

                    if (privacy == PrivacyType.Local)
                    {
                        return new LocalDefNode(name, privacy, typeRef);
                    }
                    else
                    {
                        return new FieldDefNode(name, privacy, typeRef, attributes);
                    }
                }
                else if (context.Current?.Type == TokenType.Assign)
                {
                    context.MoveNext();
                    var value = ProcessExpression(context, null);

                    if (context.Current?.Type == TokenType.Semicolon)
                    {
                        context.MoveNext();
                        return new LocalDefNode(name, privacy, typeRef, value);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.Semicolon);
                    }
                }
                else
                {
                    throw new UnexpectedTokenException(context.Current.Value);
                }
            }
            else if (privacy == PrivacyType.Local && context.Current?.Type == TokenType.InferAssign)
            {
                context.MoveNext();
                var value = ProcessExpression(context, null);

                if (context.Current?.Type == TokenType.Semicolon)
                {
                    context.MoveNext();
                    return new InferredLocalDefNode(name, privacy, value);
                }
                else
                {
                    throw new UnexpectedTokenException(context.Current.Value, TokenType.Semicolon);
                }
            }
            else
            {
                throw new UnexpectedTokenException(context.Current.Value);
            }
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
        }        
    }

    public static List<ParameterNode> ProcessParameterList(ParseContext context, out bool isVariadic)
    {
        List<ParameterNode> @params = new List<ParameterNode>();
        isVariadic = false;

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
                    var param = ProcessParameter(context, out bool b);

                    if (param == null && b)
                    {
                        isVariadic = true;

                        if (context.Current?.Type != TokenType.ClosingParentheses)
                        {
                            throw new UnexpectedTokenException(context.Current.Value, TokenType.ClosingParentheses);
                        }
                    }
                    else if (param != null)
                    {
                        @params.Add(param);
                    }

                    break;
            }
        }

        throw new UnexpectedTokenException(context.Current.Value);
    }

    public static ParameterNode ProcessParameter(ParseContext context, out bool isVariadic)
    {
        string name;
        isVariadic = false;

        if (context.Current?.Type == TokenType.Name)
        {
            name = context.Current.Value.Text.ToString();
            context.MoveNext();
        }
        else if (context.Current?.Type == TokenType.Variadic)
        {
            context.MoveNext();
            isVariadic = true;
            return null;
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
        }

        return new ParameterNode(name, ProcessTypeRef(context));
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

    public static TypeRefNode ProcessTypeRef(ParseContext context)
    {
        if (context.Current?.Type == TokenType.TypeRef)
        {
            context.MoveNext();

            if (context.Current?.Type == TokenType.Name)
            {
                string retTypeName = context.Current.Value.Text.ToString();
                bool isPointer = false;
                context.MoveNext();

                if (context.Current?.Type == TokenType.Asterix)
                {
                    isPointer = true;
                    context.MoveNext();
                }

                return new TypeRefNode(retTypeName, isPointer);
            }
            else
            {
                throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
            }
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.TypeRef);
        }
    }

    // Set lastCreatedNode to null when calling the parent, if not calling parent pass down the variable through all methods.
    public static ExpressionNode ProcessExpression(ParseContext context, ExpressionNode? lastCreatedNode, bool nullAllowed = false)
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
                case TokenType.LiteralChar:
                    lastCreatedNode = new ConstantNode(context.Current.Value.ToString()[0]);
                    context.MoveNext();
                    break;
                case TokenType.True:
                    lastCreatedNode = new ConstantNode(true);
                    context.MoveNext();
                    break;
                case TokenType.False:
                    lastCreatedNode = new ConstantNode(false);
                    context.MoveNext();
                    break;
                case TokenType.Null:
                    lastCreatedNode = new ConstantNode(null);
                    context.MoveNext();
                    break;
                case TokenType.Pi:
                    lastCreatedNode = new ConstantNode(3.14159265358979323846264f);
                    context.MoveNext();
                    break;
                case TokenType.Plus:
                    context.MoveNext();

                    if (lastCreatedNode != null)
                    {
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Addition, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.Hyphen:
                    context.MoveNext();

                    if (lastCreatedNode != null)
                    {
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Subtraction, lastCreatedNode);
                    }
                    else
                    {
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Multiplication, new ConstantNode(-1));
                    }

                    break;
                case TokenType.Asterix:
                    context.MoveNext();

                    if (lastCreatedNode != null)
                    {
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Multiplication, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.ForwardSlash:
                    context.MoveNext();

                    if (lastCreatedNode != null)
                    {
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Division, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.Exponential:
                    context.MoveNext();

                    if (lastCreatedNode != null)
                    {
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Exponential, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.Equal:
                    context.MoveNext();

                    if (lastCreatedNode != null)
                    {
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Equal, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.NotEqual:
                    context.MoveNext();

                    if (lastCreatedNode != null)
                    {
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.NotEqual, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.LargerThan:
                    context.MoveNext();

                    if (lastCreatedNode != null)
                    {
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LargerThan, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.LessThan:
                    context.MoveNext();

                    if (lastCreatedNode != null)
                    {
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LessThan, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.LargerThanOrEqual:
                    context.MoveNext();

                    if (lastCreatedNode != null)
                    {
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LargerThanOrEqual, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.LessThanOrEqual:
                    context.MoveNext();

                    if (lastCreatedNode != null)
                    {
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LessThanOrEqual, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.Or:
                    context.MoveNext();

                    if (lastCreatedNode != null)
                    {
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Or, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.And:
                    context.MoveNext();

                    if (lastCreatedNode != null)
                    {
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.And, lastCreatedNode);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.Not:
                    context.MoveNext();
                    lastCreatedNode = new InverseNode(ProcessAccess(context));
                    break;
                case TokenType.Increment:
                case TokenType.Decrement:
                    lastCreatedNode = ProcessIncrementDecrement(context);
                    break;
                case TokenType.TypeRef:
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
                    if (!nullAllowed && lastCreatedNode == null)
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    return lastCreatedNode;
            }
        }

        throw new UnexpectedTokenException(context.Current.Value);
    }

    public static RefNode ProcessAccess(ParseContext context)
    {
        RefNode newRefNode = null;

        if (context.Current?.Type == TokenType.This)
        {
            newRefNode = new ThisNode();
            context.MoveNext();

            if (context.Current?.Type == TokenType.Period)
            {
                context.MoveNext();
            }
            else
            {
                return newRefNode;
            }
        }
        else if (context.Current?.Type == TokenType.TypeRef)
        {
            newRefNode = ProcessTypeRef(context);

            if (context.Current?.Type == TokenType.Period)
            {
                context.MoveNext();
            }
            else
            {
                return newRefNode;
            }
        }

        while (context.Current != null)
        {
            if (context.Current?.Type == TokenType.Name)
            {
                string name = context.Current.Value.Text.ToString();
                RefNode currentNode = newRefNode;
                context.MoveNext();

                switch (context.Current?.Type)
                {
                    case TokenType.OpeningParentheses:
                        {
                            context.MoveNext();
                            var childNode = new MethodCallNode(name, ProcessArgs(context));

                            if (currentNode == null)
                            {
                                currentNode = childNode;
                                newRefNode = childNode;
                            }
                            else
                            {
                                currentNode.Child = childNode;
                                currentNode = currentNode.Child;
                            }

                            break;
                        }
                    case TokenType.OpeningSquareBrackets:
                        {
                            context.MoveNext();
                            var childNode = new IndexAccessNode(name, ProcessExpression(context, null));

                            if (context.Current?.Type != TokenType.ClosingSquareBrackets)
                            {
                                throw new UnexpectedTokenException(context.Current.Value, TokenType.ClosingSquareBrackets);
                            }

                            context.MoveNext();

                            if (currentNode == null)
                            {
                                currentNode = childNode;
                                newRefNode = childNode;
                            }
                            else
                            {
                                currentNode.Child = childNode;
                                currentNode = currentNode.Child;
                            }

                            break;
                        }
                    default:
                        {
                            var childNode = new RefNode(name);

                            if (currentNode == null)
                            {
                                currentNode = childNode;
                                newRefNode = childNode;
                            }
                            else
                            {
                                currentNode.Child = childNode;
                                currentNode = currentNode.Child;
                            }

                            break;
                        }
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

        if (left is BinaryOperationNode bin)
        {
            if (GetOpPriority(bin.Type) < GetOpPriority(opType))
            {
                bin.Right = new BinaryOperationNode(bin.Right, right, opType);
                return bin.Right;
            }
        }

        return new BinaryOperationNode(left, right, opType);
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
            case OperationType.Or:
            case OperationType.LessThan:
            case OperationType.LargerThan:
            case OperationType.And:
                return 1;
            case OperationType.Assignment:
                return 0;
            default:
                return 0;
        }
    }
}
