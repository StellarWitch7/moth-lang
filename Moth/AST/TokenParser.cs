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
        AssignNamespaceNode assignNamespaceNode;
        List<ImportNode> imports = new List<ImportNode>();
        List<ClassNode> classes = new List<ClassNode>();
        List<MethodDefNode> funcs = new List<MethodDefNode>();

        if (context.Current?.Type == TokenType.NamespaceTag)
        {
            context.MoveNext();
            assignNamespaceNode = ProcessNamespaceAssignment(context);
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.NamespaceTag);
        }

        while (context.Current != null)
        {
            switch (context.Current?.Type)
            {
                case TokenType.Import:
                    context.MoveNext();
                    imports.Add(ProcessImport(context));
                    break;
                case TokenType.Function:
                    context.MoveNext();
                    bool isConstant = false;
                    
                    if (context.Current?.Type == TokenType.Constant)
                    {
                        isConstant = true;
                        context.MoveNext();
                    }

                    string typeRef = context.Current.Value.Text.ToString();
                    context.MoveNext();
                    funcs.Add((MethodDefNode)ProcessDefinition(context, PrivacyType.Public, typeRef, isConstant));
                    break;
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

        return new ScriptAST(assignNamespaceNode, imports, classes, funcs);
    }

    public static NamespaceNode ProcessNamespace(ParseContext context)
    {
        List<string> @namespace = new List<string>();

        while (context.Current != null)
        {
            switch (context.Current?.Type)
            {
                case TokenType.Semicolon:
                    context.MoveNext();
                    return new NamespaceNode(@namespace);
                case TokenType.Name:
                    @namespace.Add(context.Current.Value.Text.ToString());
                    context.MoveNext();
                    break;
                case TokenType.Period:
                    context.MoveNext();
                    break;
                default:
                    throw new UnexpectedTokenException(context.Current.Value);
            }
        }

        throw new UnexpectedTokenException(context.Current.Value);
    }

    public static AssignNamespaceNode ProcessNamespaceAssignment(ParseContext context)
    {
        return new AssignNamespaceNode(ProcessNamespace(context));
    }

    public static ImportNode ProcessImport(ParseContext context)
    {
        if (context.Current?.Type == TokenType.NamespaceTag)
        {
            context.MoveNext();
            return new ImportNode(ProcessNamespace(context));
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.NamespaceTag);
        }
    }

    public static ClassNode ProcessClass(ParseContext context, PrivacyType privacy, string name)
    {
        if (context.Current?.Type == TokenType.OpeningCurlyBraces)
        {
            context.CurrentClassName = name;
            context.MoveNext();
            return new ClassNode(name, privacy, ProcessBlock(context, true));
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningCurlyBraces);
        }
    }

    public static ScopeNode ProcessBlock(ParseContext context, bool isClassRoot = false)
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
                    bool inferAssign = false;

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

                    if (context.Current?.Type == TokenType.Constant)
                    {
                        isConstant = true;
                        context.MoveNext();
                    }

                    List<TokenType> peek3List = new List<TokenType>();

                    foreach (Token token in context.Peek(2))
                    {
                        peek3List.Add(token.Type);
                    }

                    TokenType[] peek3 = peek3List.ToArray();

                    if (privacyType == PrivacyType.Local
                        && peek3.Equals(new TokenType[] { TokenType.Name, TokenType.Name, TokenType.Semicolon }))
                    {
                        string typeRef = context.Current.Value.Text.ToString();
                        context.MoveNext();
                        statements.Add(ProcessDefinition(context, privacyType, typeRef, isConstant));
                        break;
                    }
                    else
                    {
                        ExpressionNode val = ProcessExpression(context, null, TokenType.Colon);
                        statements.Add(ProcessDefinition(context, privacyType, val, isConstant));
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

                        if (context.Current?.Type == TokenType.This)
                        {
                            refNode = new ThisNode();
                            context.MoveNext();
                        }
                        else if (context.Current?.Type == TokenType.Name)
                        {
                            refNode = new RefNode(context.Current.Value.Text.ToString());
                            context.MoveNext();
                        }
                        else
                        {
                            throw new UnexpectedTokenException(context.Current.Value);
                        }

                        if (context.Current?.Type == TokenType.Period)
                        {
                            context.MoveNext();
                            refNode = ProcessAccess(context, refNode);
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
        PrivacyType privacyType, object typeRef, bool isConstant)
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

                if (context.Current?.Type == TokenType.OpeningCurlyBraces)
                {
                    context.MoveNext();
                    var statements = ProcessBlock(context);

                    return new MethodDefNode(name, privacyType, strTypeRef, @params, statements);
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

    public static List<ParameterNode> ProcessParameterList(ParseContext context)
    {
        List<ParameterNode> @params = new List<ParameterNode>();

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
        var condition = ProcessExpression(context, null, TokenType.OpeningCurlyBraces);
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
            if (context.GetByIndex(context.Position - 1).Type == TokenType.ClosingParentheses) break;
            args.Add(ProcessExpression(context, null, new TokenType[] { TokenType.ClosingParentheses, TokenType.Comma }));
        }

        return args;
    }

    public static ExpressionNode ProcessExpression(ParseContext context, ExpressionNode lastCreatedNode, TokenType terminator)
    {
        return ProcessExpression(context, lastCreatedNode, new TokenType[] { terminator });
    }

    // Set lastCreatedNode to null when calling the parent, if not calling parent pass down the variable through all methods.
    public static ExpressionNode ProcessExpression(ParseContext context, ExpressionNode lastCreatedNode, TokenType[] terminators)
    {
        bool isParent = lastCreatedNode == null;

        while (context.Current != null)
        {
            foreach (TokenType terminator in terminators)
            {
                if (context.Current?.Type == terminator)
                {
                    if (isParent) context.MoveNext();
                    return lastCreatedNode;
                }
            }

            switch (context.Current?.Type)
            {
                case TokenType.OpeningParentheses:
                    context.MoveNext();
                    lastCreatedNode = ProcessExpression(context, lastCreatedNode, TokenType.ClosingParentheses);
                    return lastCreatedNode;
                case TokenType.ClosingSquareBrackets:
                    if (isParent) context.MoveNext();
                    return lastCreatedNode;
                case TokenType.LiteralFloat:
                    lastCreatedNode = new ConstantNode(float.Parse(context.Current.Value.Text.Span));
                    context.MoveNext();
                    break;
                case TokenType.LiteralInt:
                    lastCreatedNode = new ConstantNode(int.Parse(context.Current.Value.Text.Span));
                    context.MoveNext();
                    break;
                case TokenType.LiteralString:
                    lastCreatedNode = new ConstantNode(context.Current.Value.Text);
                    context.MoveNext();
                    break;
                case TokenType.New:
                    context.MoveNext();

                    if (context.Current?.Type != TokenType.Name)
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
                    }

                    lastCreatedNode = ProcessNew(context);
                    break;
                case TokenType.Addition:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Addition, lastCreatedNode, terminators);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.Subtraction:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Subtraction, lastCreatedNode, terminators);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.Multiplication:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Multiplication, lastCreatedNode, terminators);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.Division:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Division, lastCreatedNode, terminators);
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
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Exponential, lastCreatedNode, terminators);
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
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Equal, lastCreatedNode, terminators);
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
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.NotEqual, lastCreatedNode, terminators);
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
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LargerThan, lastCreatedNode, terminators);
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
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LessThan, lastCreatedNode, terminators);
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
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LargerThanOrEqual, lastCreatedNode, terminators);
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
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LessThanOrEqual, lastCreatedNode, terminators);
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
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LogicalOr, lastCreatedNode, terminators);
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
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LogicalXor, lastCreatedNode, terminators);
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
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LogicalAnd, lastCreatedNode, terminators);
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
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.LogicalNand, lastCreatedNode, terminators);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                case TokenType.Name:
                    string name = context.Current.Value.Text.ToString();
                    context.MoveNext();
                    lastCreatedNode = ProcessAccess(context, new RefNode(name));
                    break;
                case TokenType.This:
                    context.MoveNext();
                    lastCreatedNode = ProcessAccess(context, new ThisNode());
                    break;
                case TokenType.AssignmentSeparator:
                    if (lastCreatedNode != null)
                    {
                        context.MoveNext();
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Assignment, lastCreatedNode, terminators);
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    break;
                default:
                    throw new UnexpectedTokenException(context.Current.Value);
            }
        }

        if (lastCreatedNode == null)
        {
            throw new UnexpectedTokenException(context.Current.Value);
        }

        return lastCreatedNode;
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

    public static RefNode ProcessAccess(ParseContext context, RefNode refNode)
    {
        RefNode newRefNode = refNode;

        if (context.Current?.Type == TokenType.Period)
        {
            context.MoveNext();
        }

        while (context.Current != null)
        {
            if (context.Current?.Type == TokenType.Name)
            {
                string name = context.Current.Value.Text.ToString();
                context.MoveNext();

                switch (context.Current?.Type)
                {
                    case TokenType.OpeningParentheses:
                        context.MoveNext();
                        newRefNode.Child = new MethodCallNode(name, ProcessArgs(context));
                        break;
                    case TokenType.OpeningSquareBrackets:
                        context.MoveNext();
                        newRefNode.Child = new IndexAccessNode(ProcessExpression(context, null, TokenType.ClosingSquareBrackets)); //TODO: check bool validity
                        newRefNode = newRefNode.Child;
                        break;
                    default:
                        newRefNode.Child = new RefNode(name);
                        break;
                }

                newRefNode = newRefNode.Child;

                if (context.Current?.Type == TokenType.Period)
                {
                    context.MoveNext();
                }
                else
                {
                    return newRefNode;
                }
            }
        }

        throw new UnexpectedTokenException(context.Current.Value);
    }

    public static ExpressionNode ProcessBinaryOp(ParseContext context, OperationType opType,
        ExpressionNode left, TokenType[] terminators)
    {
        var right = ProcessExpression(context, left, terminators);

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
