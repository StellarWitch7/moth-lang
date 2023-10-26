using System.Text;
using Moth.Tokens;
using Moth.AST.Node;

namespace Moth.AST; //TODO: allow calling functions on expressions

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
        if (context.Current?.Type == TokenType.OpeningGenericBracket)
        {
            List<GenericParameterNode> @params = new List<GenericParameterNode>();
            context.MoveNext();

            while (context.Current != null)
            {
                @params.Add(ProcessGenericParam(context));

                if (context.Current?.Type == TokenType.Comma)
                {
                    context.MoveNext();
                }
                else if (context.Current?.Type == TokenType.ClosingGenericBracket)
                {
                    context.MoveNext();

                    if (context.Current?.Type == TokenType.OpeningCurlyBraces)
                    {
                        context.MoveNext();
                        return new GenericClassNode(name, privacy, @params, ProcessScope(context, true));
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningCurlyBraces);
                    }
                }
                else
                {
                    throw new UnexpectedTokenException(context.Current.Value);
                }
            }

            throw new UnexpectedTokenException(context.Current.Value, TokenType.ClosingGenericBracket);
        }
        else
        {
            if (context.Current?.Type == TokenType.OpeningCurlyBraces)
            {
                context.MoveNext();
                return new ClassNode(name, privacy, ProcessScope(context, true));
            }
            else
            {
                throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningCurlyBraces);
            }
        }
    }

    private static GenericParameterNode ProcessGenericParam(ParseContext context)
    {
        if (context.Current?.Type != TokenType.Name)
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
        }

        string name = context.Current.Value.Text.ToString();
        context.MoveNext();

        if (context.Current?.Type != TokenType.TypeRef)
        {
            return new GenericParameterNode(name);
        }

        var typeRef = ProcessTypeRef(context);
        return new ConstGenericParameterNode(name, typeRef);
    }

    public static ScopeNode ProcessScope(ParseContext context, bool isClassRoot = false)
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
                        statements.Add(ProcessScope(context));
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
                    default:
                        statements.Add(ProcessExpression(context, null));

                        if (context.Current?.Type == TokenType.Semicolon)
                        {
                            context.MoveNext();
                            break;
                        }
                        else
                        {
                            throw new UnexpectedTokenException(context.Current.Value, TokenType.Semicolon);
                        }
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

        if (type == TokenType.Increment)
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
        var then = ProcessScope(context);
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

        throw new NotImplementedException();
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
                    return new FuncDefNode(name, privacy, retTypeRef, @params, ProcessScope(context), isVariadic, attributes);
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
            else
            {
                var typeRef = ProcessTypeRef(context);

                if (context.Current?.Type == TokenType.Semicolon)
                {
                    context.MoveNext();
                    return new FieldDefNode(name, privacy, typeRef, attributes);
                }
                else
                {
                    throw new UnexpectedTokenException(context.Current.Value, TokenType.Semicolon);
                }
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
        bool requireRefType = false;
        isVariadic = false;

        if (context.Current?.Type == TokenType.Ref)
        {
            requireRefType = true;
            context.MoveNext();
        }

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

        return new ParameterNode(name, ProcessTypeRef(context), requireRefType);
    }

    public static IfNode ProcessIf(ParseContext context)
    {
        var condition = ProcessExpression(context, null);

        if (context.Current?.Type != TokenType.OpeningCurlyBraces)
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningCurlyBraces);
        }

        context.MoveNext();
        var then = ProcessScope(context);
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
                return ProcessScope(context);
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
        if (context.Current?.Type == TokenType.TypeRef
            || context.Current?.Type == TokenType.GenericTypeRef) //TODO: handle this patheticness
        {
            var startToken = context.Current?.Type;
            context.MoveNext();

            if (context.Current?.Type == TokenType.Name)
            {
                string retTypeName = context.Current.Value.Text.ToString();
                var genericParams = new List<ExpressionNode>();
                var pointerDepth = 0;
                context.MoveNext();

                if (startToken != TokenType.GenericTypeRef
                    && context.Current?.Type == TokenType.OpeningGenericBracket)
                {
                    context.MoveNext();

                    while (context.Current?.Type != TokenType.ClosingGenericBracket)
                    {
                        if (context.Current?.Type == TokenType.TypeRef
                            || context.Current?.Type == TokenType.GenericTypeRef)
                        {
                            genericParams.Add(ProcessTypeRef(context));
                        }
                        else
                        {
                            genericParams.Add(ProcessExpression(context, null));
                        }

                        if (context.Current?.Type == TokenType.Comma)
                        {
                            context.MoveNext();
                        }
                        else if (context.Current?.Type != TokenType.ClosingGenericBracket)
                        {
                            throw new UnexpectedTokenException(context.Current.Value);
                        }
                    }

                    context.MoveNext();
                }

                while (context.Current?.Type == TokenType.Asterix)
                {
                    pointerDepth++;
                    context.MoveNext();
                }

                if (genericParams.Count != 0)
                {
                    return new GenericTypeRefNode(retTypeName, genericParams, pointerDepth);
                }
                else
                {
                    return new TypeRefNode(retTypeName, pointerDepth);
                }
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
                    lastCreatedNode = new SubExprNode(ProcessExpression(context, lastCreatedNode));

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
                    lastCreatedNode = new ConstantNode(context.Current.Value.Text.ToString()[0]);
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
                case TokenType.Ref:
                    context.MoveNext();
                    lastCreatedNode = new ReferenceNode(ProcessExpression(context, null));
                    break;
                case TokenType.Period:
                    throw new NotImplementedException("Access operations on expressions are not currently supported."); //TODO
                case TokenType.Local:
                    context.MoveNext();

                    if (context.Current?.Type == TokenType.Name)
                    {
                        string name = context.Current.Value.Text.ToString();
                        context.MoveNext();

                        if (context.Current?.Type == TokenType.InferAssign)
                        {
                            context.MoveNext();
                            lastCreatedNode = new InferredLocalDefNode(name, ProcessExpression(context, null));
                        }
                        else
                        {
                            var type = ProcessTypeRef(context);
                            lastCreatedNode = new LocalDefNode(name, type);
                        }
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
                    }

                    break;
                case TokenType.If:
                    context.MoveNext();
                    var condition = ProcessExpression(context, null);

                    if (context.Current?.Type != TokenType.Then)
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.Then);
                    }

                    context.MoveNext();
                    var then = ProcessExpression(context, null);

                    if (context.Current?.Type != TokenType.Else)
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.Else);
                    }

                    context.MoveNext();
                    var @else = ProcessExpression(context, null);

                    lastCreatedNode = new InlineIfNode(condition, then, @else);
                    break;
                case TokenType.AddAssign:
                    context.MoveNext();

                    if (lastCreatedNode != null)
                    {
                        lastCreatedNode = new BinaryOperationNode(lastCreatedNode,
                            ProcessBinaryOp(context, OperationType.Addition, lastCreatedNode),
                            OperationType.Assignment);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.SubAssign:
                    context.MoveNext();

                    if (lastCreatedNode != null)
                    {
                        lastCreatedNode = new BinaryOperationNode(lastCreatedNode,
                            ProcessBinaryOp(context, OperationType.Subtraction, lastCreatedNode),
                            OperationType.Assignment);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.Cast:
                    context.MoveNext();

                    if (lastCreatedNode != null && lastCreatedNode is TypeRefNode)
                    {
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Cast, lastCreatedNode);
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
                case TokenType.Plus:
                case TokenType.Asterix:
                case TokenType.ForwardSlash:
                case TokenType.Exponential:
                case TokenType.Equal:
                case TokenType.NotEqual:
                case TokenType.GreaterThan:
                case TokenType.LesserThan:
                case TokenType.GreaterThanOrEqual:
                case TokenType.LesserThanOrEqual:
                case TokenType.Or:
                case TokenType.And:
                    OperationType opType = context.Current?.Type switch
                    {
                        TokenType.Plus => OperationType.Addition,
                        TokenType.Asterix => OperationType.Multiplication,
                        TokenType.ForwardSlash => OperationType.Exponential,
                        TokenType.Equal => OperationType.Equal,
                        TokenType.NotEqual => OperationType.NotEqual,
                        TokenType.GreaterThan => OperationType.GreaterThan,
                        TokenType.LesserThan => OperationType.LesserThan,
                        TokenType.GreaterThanOrEqual => OperationType.GreaterThanOrEqual,
                        TokenType.LesserThanOrEqual => OperationType.LesserThanOrEqual,
                        TokenType.Or => OperationType.Or,
                        TokenType.And => OperationType.And,
                        _ => throw new UnexpectedTokenException(context.Current.Value)
                    };
                    context.MoveNext();

                    if (lastCreatedNode != null)
                    {
                        lastCreatedNode = ProcessBinaryOp(context, opType, lastCreatedNode);
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
                case TokenType.GenericTypeRef:
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
        else if (context.Current?.Type == TokenType.TypeRef
            || context.Current?.Type == TokenType.GenericTypeRef)
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
                            var childNode = new FuncCallNode(name, ProcessArgs(context));

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
        var right = ProcessExpression(context, null);

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
                return 5;
            case OperationType.Modulo:
            case OperationType.Multiplication:
            case OperationType.Division:
                return 4;
            case OperationType.Addition:
            case OperationType.Subtraction:
                return 3;
            case OperationType.Equal:
            case OperationType.NotEqual:
            case OperationType.LesserThanOrEqual:
            case OperationType.GreaterThanOrEqual:
            case OperationType.Or:
            case OperationType.LesserThan:
            case OperationType.GreaterThan:
            case OperationType.And:
                return 2;
            case OperationType.Cast:
                return 1;
            default:
                return 0;
        }
    }
}
