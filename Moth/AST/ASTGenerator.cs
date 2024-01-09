using Moth.AST.Node;
using Moth.LLVM;
using Moth.LLVM.Data;
using Moth.Tokens;
using System.Runtime.CompilerServices;

namespace Moth.AST; //TODO: allow calling functions on expressions

public static class ASTGenerator
{
    public static ScriptAST ProcessScript(ParseContext context)
    {
        string @namespace;
        var imports = new List<string>();
        var funcs = new List<FuncDefNode>();
        var globals = new List<FieldDefNode>();
        var classes = new List<ClassNode>();

        if (context.Current?.Type == TokenType.Namespace)
        {
            context.MoveNext();
            @namespace = ProcessNamespace(context);
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Namespace);
        }

        var attributes = new List<AttributeNode>();

        while (context.Current != null)
        {
            switch (context.Current?.Type)
            {
                case TokenType.AttributeMarker:
                    attributes.Add(ProcessAttribute(context));
                    break;
                case TokenType.Import:
                    context.MoveNext();
                    imports.Add(ProcessNamespace(context));
                    break;
                case TokenType.Public:
                case TokenType.Private:
                    StatementNode result = ProcessDefinition(context, attributes);
                    attributes = new List<AttributeNode>();

                    if (result is ClassNode @class)
                    {
                        classes.Add(@class);
                    }
                    else if (result is FuncDefNode func)
                    {
                        funcs.Add(func);
                    }
                    else if (result is FieldDefNode global)
                    {
                        globals.Add(global);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    break;
                default:
                    throw new UnexpectedTokenException(context.Current.Value);
            }
        }

        return new ScriptAST(@namespace, imports, classes, funcs, globals);
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

    public static ClassNode ProcessClass(ParseContext context, PrivacyType privacy, bool isForeign, bool isStruct = false)
    {
        if ((isStruct && context.Current?.Type != TokenType.Struct) || (!isStruct && context.Current?.Type != TokenType.Class))
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Class);
        }
        
        if (context.MoveNext()?.Type == TokenType.Name)
        {
            string name = context.Current.Value.Text.ToString();
            context.MoveNext();
            
            if (!isStruct && !isForeign && context.Current?.Type == TokenType.OpeningGenericBracket)
            {
                var @params = new List<GenericParameterNode>();
                context.MoveNext();

                while (context.Current != null)
                {
                    @params.Add(ProcessGenericParam(context));

                    if (context.Current?.Type == TokenType.Comma)
                    {
                        context.MoveNext();
                    }
                    else
                    {
                        return context.Current?.Type == TokenType.ClosingGenericBracket
                            ? context.MoveNext()?.Type == TokenType.OpeningCurlyBraces
                                ? (ClassNode)new GenericClassNode(name, privacy, @params, ProcessScope(context, true))
                                : throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningCurlyBraces)
                            : throw new UnexpectedTokenException(context.Current.Value);
                    }
                }

                throw new UnexpectedTokenException(context.Current.Value, TokenType.ClosingGenericBracket);
            }
            else
            {
                if (isForeign)
                {
                    if (context.Current?.Type == TokenType.Semicolon)
                    {
                        context.MoveNext();
                        return new ClassNode(name, privacy, null, isStruct);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.Semicolon);
                    }
                }
                else
                {
                    return new ClassNode(name, privacy, ProcessScope(context, true), isStruct);
                }
            }
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
        }
    }

    private static GenericParameterNode ProcessGenericParam(ParseContext context)
    {
        if (context.Current?.Type != TokenType.Name)
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
        }

        string name = context.Current.Value.Text.ToString();

        if (context.MoveNext()?.Type != TokenType.TypeRef)
        {
            return new GenericParameterNode(name);
        }

        TypeRefNode typeRef = ProcessTypeRef(context);
        return new ConstGenericParameterNode(name, typeRef);
    }

    public static ScopeNode ProcessScope(ParseContext context, bool isClassRoot = false)
    {
        var statements = new List<StatementNode>();

        if (context.Current?.Type != TokenType.OpeningCurlyBraces)
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningCurlyBraces);
        }

        context.MoveNext();

        if (isClassRoot)
        {
            var attributes = new List<AttributeNode>();

            while (context.Current != null)
            {
                switch (context.Current?.Type)
                {
                    case TokenType.ClosingCurlyBraces:
                        context.MoveNext();
                        return new ScopeNode(statements);
                    case TokenType.AttributeMarker:
                        attributes.Add(ProcessAttribute(context));
                        break;
                    case TokenType.Public:
                    case TokenType.Private:
                        StatementNode newDef = ProcessDefinition(context, attributes);
                        attributes = new List<AttributeNode>();
                        statements.Add(newDef);
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
        var type = (TokenType)(context.Current?.Type);
        RefNode refNode = context.MoveNext()?.Type == TokenType.This || context.Current?.Type == TokenType.Name
            ? ProcessAccess(context)
            : throw new UnexpectedTokenException(context.Current.Value);
        return type == TokenType.Increment
            ? new IncrementVarNode(refNode)
            : new DecrementVarNode(refNode);
    }

    public static StatementNode ProcessWhile(ParseContext context)
    {
        ExpressionNode condition = ProcessExpression(context, null);

        if (context.Current?.Type != TokenType.OpeningCurlyBraces)
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningCurlyBraces);
        }

        ScopeNode then = ProcessScope(context);
        return new WhileNode(condition, then);
    }

    public static AttributeNode ProcessAttribute(ParseContext context)
    {
        if (context.Current?.Type == TokenType.AttributeMarker)
        {
            if (context.MoveNext()?.Type == TokenType.Name)
            {
                string name = context.Current.Value.Text.ToString();

                if (context.MoveNext()?.Type == TokenType.OpeningParentheses)
                {
                    context.MoveNext();
                    return new AttributeNode(name, ProcessArgs(context));
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

    public static StatementNode ProcessDefinition(ParseContext context, List<AttributeNode>? attributes = null)
    {
        PrivacyType privacy = context.Current?.Type == TokenType.Public
            ? PrivacyType.Public
            : context.Current?.Type == TokenType.Private
                ? PrivacyType.Private
                : throw new UnexpectedTokenException(context.Current.Value);
        bool isForeign = false;
        bool isStatic = false;

        context.MoveNext();
        
        while (context.Current?.Type == TokenType.Foreign || context.Current?.Type == TokenType.Static)
        {
            switch (context.Current?.Type)
            {
                case TokenType.Foreign:
                    if (isForeign) throw new UnexpectedTokenException(context.Current.Value);
                    isForeign = true;
                    break;
                case TokenType.Static:
                    if (isStatic) throw new UnexpectedTokenException(context.Current.Value);
                    isStatic = true;
                    break;
                default:
                    throw new NotImplementedException();
            }

            context.MoveNext();
        }

        if (context.Current?.Type == TokenType.Struct)
        {
            return ProcessClass(context, privacy, isForeign, true);
        }
        else if (context.Current?.Type == TokenType.Class)
        {
            return ProcessClass(context, privacy, isForeign);
        }
        else if (context.Current?.Type == TokenType.Function)
        {
            if (context.MoveNext()?.Type == TokenType.Name)
            {
                string name = context.Current.Value.Text.ToString();
                
                if (context.MoveNext()?.Type == TokenType.OpeningParentheses)
                {
                    context.MoveNext();
                    List<ParameterNode> @params = ProcessParameterList(context, out bool isVariadic);
                    TypeRefNode retTypeRef = context.Current?.Type == TokenType.OpeningCurlyBraces
                        || context.Current?.Type == TokenType.Semicolon
                            ? new TypeRefNode(Reserved.Void, 0)
                            : ProcessTypeRef(context);

                    if (!isForeign && context.Current?.Type == TokenType.OpeningCurlyBraces)
                    {
                        return new FuncDefNode(name,
                            privacy,
                            retTypeRef,
                            @params,
                            ProcessScope(context),
                            isVariadic,
                            isStatic,
                            isForeign,
                            attributes);
                    }
                    else if (isForeign && context.Current?.Type == TokenType.Semicolon)
                    {
                        context.MoveNext();
                        return new FuncDefNode(name,
                            privacy,
                            retTypeRef,
                            @params,
                            null,
                            isVariadic,
                            isStatic,
                            isForeign,
                            attributes);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningCurlyBraces);
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
        }
        else if (context.Current?.Type == TokenType.Name)
        {
            string name = context.Current.Value.Text.ToString();
            context.MoveNext();
            TypeRefNode typeRef = ProcessTypeRef(context);

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
        else
        {
            throw new UnexpectedTokenException(context.Current.Value);
        }
    }

    public static List<ParameterNode> ProcessParameterList(ParseContext context, out bool isVariadic)
    {
        var @params = new List<ParameterNode>();
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
                    ParameterNode? param = ProcessParameter(context, out bool b);

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

    public static ParameterNode? ProcessParameter(ParseContext context, out bool isVariadic)
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
            isVariadic = true;
            context.MoveNext();
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
        ExpressionNode condition = ProcessExpression(context, null);

        if (context.Current?.Type != TokenType.OpeningCurlyBraces)
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningCurlyBraces);
        }

        ScopeNode then = ProcessScope(context);
        ScopeNode? @else = ProcessElse(context);
        return new IfNode(condition, then, @else);
    }

    public static ScopeNode? ProcessElse(ParseContext context)
    {
        if (context.Current?.Type == TokenType.Else)
        {
            if (context.MoveNext()?.Type == TokenType.If)
            {
                context.MoveNext();
                return new ScopeNode(new List<StatementNode>
                {
                    ProcessIf(context) //TODO: this does not work in compilation
                });
            }
            else
            {
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
        var args = new List<ExpressionNode>();

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
        if (context.Current?.Type is TokenType.TypeRef
            or TokenType.GenericTypeRef) //TODO: handle this patheticness
        {
            TokenType? startToken = context.Current?.Type;

            if (context.MoveNext()?.Type == TokenType.Name)
            {
                string retTypeName = context.Current.Value.Text.ToString();
                var genericParams = new List<ExpressionNode>();
                uint pointerDepth = 0;

                if (context.MoveNext()?.Type == TokenType.OpeningGenericBracket
                    && startToken != TokenType.GenericTypeRef)
                {
                    context.MoveNext();

                    while (context.Current?.Type != TokenType.ClosingGenericBracket)
                    {
                        if (context.Current?.Type is TokenType.TypeRef
                            or TokenType.GenericTypeRef)
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
                }

                while (context.Current?.Type == TokenType.Asterix)
                {
                    pointerDepth++;
                    context.MoveNext();
                }

                return genericParams.Count != 0
                    ? new GenericTypeRefNode(retTypeName, genericParams, pointerDepth)
                    : new TypeRefNode(retTypeName, pointerDepth);
            }
            else if (startToken != TokenType.GenericTypeRef && context.Current?.Type == TokenType.OpeningParentheses)
            {
                var @params = new List<TypeRefNode>();
                uint pointerDepth = 0;
                TypeRefNode retType;
                
                while (context.MoveNext()?.Type is TokenType.TypeRef or TokenType.GenericTypeRef)
                {
                    @params.Add(ProcessTypeRef(context));

                    if (context.Current?.Type == TokenType.ClosingParentheses)
                    {
                        break;
                    }
                    else if (context.Current?.Type != TokenType.Comma)
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.ClosingParentheses);
                    }
                }

                while (context.MoveNext()?.Type == TokenType.Asterix)
                {
                    pointerDepth++;
                }

                if (context.Current?.Type == TokenType.TypeRef)
                {
                    retType = ProcessTypeRef(context);
                    return new FuncTypeRefNode(retType, @params, pointerDepth);
                }
                else
                {
                    throw new UnexpectedTokenException(context.Current.Value, TokenType.TypeRef);
                }
            }
            else if (startToken != TokenType.GenericTypeRef && context.Current?.Type == TokenType.OpeningSquareBrackets)
            {
                uint pointerDepth = 0;
                
                if (!(context.MoveNext()?.Type is TokenType.TypeRef or TokenType.GenericTypeRef))
                {
                    throw new UnexpectedTokenException(context.Current.Value);
                }

                var elementType = ProcessTypeRef(context);

                if (context.Current?.Type != TokenType.ClosingSquareBrackets)
                {
                    throw new UnexpectedTokenException(context.Current.Value, TokenType.ClosingSquareBrackets);
                }

                context.MoveNext();
                
                while (context.Current?.Type == TokenType.Asterix)
                {
                    pointerDepth++;
                    context.MoveNext();
                }

                return new ArrayTypeRefNode(elementType, pointerDepth);
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
    public static ExpressionNode ProcessExpression(ParseContext context, ExpressionNode? lastCreatedNode,
        bool nullAllowed = false, int opPriority = 0)
    {
        while (context.Current != null)
        {
            switch (context.Current?.Type)
            {
                case TokenType.OpeningParentheses:
                {
                    context.MoveNext();
                    var prevNode = lastCreatedNode;
                    lastCreatedNode = new SubExprNode(ProcessExpression(context, lastCreatedNode));

                    if (context.Current?.Type != TokenType.ClosingParentheses)
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.ClosingParentheses);
                    }

                    if (prevNode is TypeRefNode typeRef)
                    {
                        lastCreatedNode = new CastNode(typeRef, lastCreatedNode);
                    }
                    
                    context.MoveNext();
                    break;
                }
                case TokenType.LiteralFloat:
                    lastCreatedNode = new LiteralNode(float.Parse(context.Current.Value.Text.Span));
                    context.MoveNext();
                    break;
                case TokenType.LiteralInt:
                    lastCreatedNode = new LiteralNode(int.Parse(context.Current.Value.Text.Span));
                    context.MoveNext();
                    break;
                case TokenType.LiteralString:
                    lastCreatedNode = new LiteralNode(context.Current.Value.Text.ToString());
                    context.MoveNext();
                    break;
                case TokenType.LiteralChar:
                    lastCreatedNode = new LiteralNode(context.Current.Value.Text.ToString()[0]);
                    context.MoveNext();
                    break;
                case TokenType.True:
                    lastCreatedNode = new LiteralNode(true);
                    context.MoveNext();
                    break;
                case TokenType.False:
                    lastCreatedNode = new LiteralNode(false);
                    context.MoveNext();
                    break;
                case TokenType.Null:
                    lastCreatedNode = new LiteralNode(null);
                    context.MoveNext();
                    break;
                case TokenType.Pi:
                    lastCreatedNode = new LiteralNode(3.14159265358979323846264f);
                    context.MoveNext();
                    break;
                case TokenType.AddressOf:
                    context.MoveNext();
                    lastCreatedNode = new AddressOfNode(ProcessExpression(context, null));
                    break;
                case TokenType.DeRef:
                    context.MoveNext();
                    lastCreatedNode = new LoadNode(ProcessExpression(context, null));
                    break;
                case TokenType.Function:
                    if (context.MoveNext()?.Type == TokenType.OpeningParentheses)
                    {
                        context.MoveNext();
                        List<ParameterNode> @params = ProcessParameterList(context, out bool isVariadic);
                        TypeRefNode retType = ProcessTypeRef(context);

                        if (isVariadic)
                        {
                            throw new Exception($"{new UnexpectedTokenException(context.Previous().Value).Message}" +
                                $"\nCannot have a variadic locally-defined function.");
                        }

                        if (context.Current?.Type == TokenType.OpeningCurlyBraces)
                        {
                            ScopeNode scope = ProcessScope(context);
                            lastCreatedNode = new LocalFuncDefNode(retType, @params, scope);
                        }
                        else
                        {
                            throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningCurlyBraces);
                        }
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningParentheses);
                    }

                    break;
                case TokenType.Period:
                    throw new NotImplementedException("Access operations on expressions are not currently supported."); //TODO
                case TokenType.Local:
                    if (context.MoveNext()?.Type == TokenType.Name)
                    {
                        string name = context.Current.Value.Text.ToString();

                        if (context.MoveNext()?.Type == TokenType.InferAssign)
                        {
                            context.MoveNext();
                            lastCreatedNode = new InferredLocalDefNode(name, ProcessExpression(context, null));
                        }
                        else
                        {
                            TypeRefNode type = ProcessTypeRef(context);
                            lastCreatedNode = new LocalDefNode(name, type);
                        }
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
                    }

                    break;
                case TokenType.OpeningSquareBrackets:
                    {
                        if (lastCreatedNode is not TypeRefNode typeRef)
                        {
                            throw new UnexpectedTokenException(context.Current.Value);
                        }

                        lastCreatedNode = ProcessLiteralArray(context, typeRef);
                        break;
                    }
                case TokenType.If:
                    context.MoveNext();
                    ExpressionNode condition = ProcessExpression(context, null);

                    if (context.Current?.Type != TokenType.Then)
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.Then);
                    }

                    context.MoveNext();
                    ExpressionNode then = ProcessExpression(context, null);

                    if (context.Current?.Type != TokenType.Else)
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.Else);
                    }

                    context.MoveNext();
                    ExpressionNode @else = ProcessExpression(context, null);

                    lastCreatedNode = new InlineIfNode(condition, then, @else);
                    break;
                case TokenType.Hyphen:
                    if (lastCreatedNode != null)
                    {
                        goto case TokenType.Assign;
                    }
                    else
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Multiplication, new LiteralNode(-1));
                        break;
                    }
                case TokenType.Assign:
                case TokenType.Plus:
                case TokenType.Asterix:
                case TokenType.ForwardSlash:
                case TokenType.Modulo:
                case TokenType.Exponential:
                case TokenType.Equal:
                case TokenType.NotEqual:
                case TokenType.GreaterThan:
                case TokenType.LesserThan:
                case TokenType.GreaterThanOrEqual:
                case TokenType.LesserThanOrEqual:
                case TokenType.Or:
                case TokenType.And:
                    {
                        OperationType opType = TokenToOpType(context, context.Current?.Type);

                        if (lastCreatedNode != null)
                        {
                            if (GetOpPriority(opType) < opPriority)
                            {
                                return lastCreatedNode;
                            }

                            context.MoveNext();
                            lastCreatedNode = ProcessBinaryOp(context, opType, lastCreatedNode);
                        }
                        else
                        {
                            throw new UnexpectedTokenException(context.Current.Value);
                        }

                        break;
                    }
                case TokenType.AddAssign:
                case TokenType.SubAssign:
                case TokenType.MulAssign:
                case TokenType.DivAssign:
                case TokenType.ModAssign:
                case TokenType.ExpAssign:
                    {
                        OperationType opType = TokenToOpType(context, context.Current?.Type);
                        context.MoveNext();

                        lastCreatedNode = lastCreatedNode != null
                            ? new BinaryOperationNode(lastCreatedNode,
                                ProcessBinaryOp(context, opType, lastCreatedNode),
                                OperationType.Assignment)
                            : throw new UnexpectedTokenException(context.Current.Value);

                        break;
                    }
                case TokenType.ScientificNotation:
                    context.MoveNext();
                    lastCreatedNode = new BinaryOperationNode(lastCreatedNode,
                        ProcessBinaryOp(context, OperationType.Exponential, new LiteralNode(10)),
                        OperationType.Multiplication);
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

    private static OperationType TokenToOpType(ParseContext context, TokenType? type)
    {
        return type switch
        {
            TokenType.Assign => OperationType.Assignment,
            TokenType.Plus => OperationType.Addition,
            TokenType.Hyphen => OperationType.Subtraction,
            TokenType.Asterix => OperationType.Multiplication,
            TokenType.ForwardSlash => OperationType.Division,
            TokenType.Modulo => OperationType.Modulo,
            TokenType.Exponential => OperationType.Exponential,
            TokenType.Equal => OperationType.Equal,
            TokenType.NotEqual => OperationType.NotEqual,
            TokenType.GreaterThan => OperationType.GreaterThan,
            TokenType.LesserThan => OperationType.LesserThan,
            TokenType.GreaterThanOrEqual => OperationType.GreaterThanOrEqual,
            TokenType.LesserThanOrEqual => OperationType.LesserThanOrEqual,
            TokenType.Or => OperationType.Or,
            TokenType.And => OperationType.And,
            TokenType.AddAssign => OperationType.Addition,
            TokenType.SubAssign => OperationType.Subtraction,
            TokenType.MulAssign => OperationType.Multiplication,
            TokenType.DivAssign => OperationType.Division,
            TokenType.ModAssign => OperationType.Modulo,
            TokenType.ExpAssign => OperationType.Exponential,
            _ => throw new UnexpectedTokenException(context.Current.Value)
        };
    }

    public static LiteralArrayNode ProcessLiteralArray(ParseContext context, TypeRefNode elementType)
    {
        var elements = new List<ExpressionNode>();
        
        if (context.Current?.Type != TokenType.OpeningSquareBrackets)
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningSquareBrackets);
        }

        context.MoveNext();
        bool b = true;
            
        while (b)
        {
            switch (context.Current?.Type)
            {
                case TokenType.ClosingSquareBrackets:
                    b = false;
                    break;
                case TokenType.Comma:
                    context.MoveNext();
                    break;
                default:
                    elements.Add(ProcessExpression(context, null));
                    break;
            }
        }

        if (context.Current?.Type != TokenType.ClosingSquareBrackets)
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.ClosingSquareBrackets);
        }

        context.MoveNext();
        return new LiteralArrayNode(elementType, elements.ToArray());
    }
    
    public static RefNode ProcessAccess(ParseContext context)
    {
        RefNode? newRefNode = null;

        if (context.Current?.Type == TokenType.This)
        {
            newRefNode = new ThisNode();

            if (context.MoveNext()?.Type == TokenType.Period)
            {
                context.MoveNext();
            }
            else
            {
                return newRefNode;
            }
        }
        else if (context.Current?.Type is TokenType.TypeRef
            or TokenType.GenericTypeRef)
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

                switch (context.MoveNext()?.Type)
                {
                    case TokenType.OpeningParentheses:
                        {
                            context.MoveNext();
                            var childNode = new FuncCallNode(name, ProcessArgs(context));

                            if (currentNode == null)
                            {
                                newRefNode = childNode;
                            }
                            else
                            {
                                currentNode.Child = childNode;
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
                                newRefNode = childNode;
                            }
                            else
                            {
                                currentNode.Child = childNode;
                            }

                            break;
                        }
                    default:
                        {
                            var childNode = new RefNode(name);

                            if (currentNode == null)
                            {
                                newRefNode = childNode;
                            }
                            else
                            {
                                currentNode.Child = childNode;
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
        ExpressionNode right = ProcessExpression(context, null, false, GetOpPriority(opType));

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
        return operationType switch
        {
            OperationType.Exponential => 5,
            OperationType.Modulo
                or OperationType.Multiplication
                or OperationType.Division => 4,
            OperationType.Addition
                or OperationType.Subtraction => 3,
            OperationType.Equal
                or OperationType.NotEqual
                or OperationType.LesserThanOrEqual
                or OperationType.GreaterThanOrEqual
                or OperationType.LesserThan
                or OperationType.GreaterThan => 2,
            OperationType.Or
                or OperationType.And => 1,
            _ => 0,
        };
    }
}
