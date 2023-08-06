using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Moth.Tokens;
using System.Collections;

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

                    switch (context.Current?.Type)
                    {
                        case TokenType.Bool:
                            context.MoveNext();
                            funcs.Add((MethodDefNode)ProcessDefinition(context, PrivacyType.Public, false,
                                DefinitionType.Bool));
                            break;
                        case TokenType.String:
                            context.MoveNext();
                            funcs.Add((MethodDefNode)ProcessDefinition(context, PrivacyType.Public, false,
                                DefinitionType.String));
                            break;
                        case TokenType.Int32:
                            context.MoveNext();
                            funcs.Add((MethodDefNode)ProcessDefinition(context, PrivacyType.Public, false,
                                DefinitionType.Int32));
                            break;
                        case TokenType.Float32:
                            context.MoveNext();
                            funcs.Add((MethodDefNode)ProcessDefinition(context, PrivacyType.Public, false,
                                DefinitionType.Float32));
                            break;
                        case TokenType.Void:
                            context.MoveNext();
                            funcs.Add((MethodDefNode)ProcessDefinition(context, PrivacyType.Public, false,
                                DefinitionType.Void));
                            break;
                        case TokenType.Name:
                            string name = context.Current.Value.Text.ToString();
                            context.MoveNext();
                            funcs.Add((MethodDefNode)ProcessDefinition(context, PrivacyType.Public, false,
                                DefinitionType.ClassObject, new ClassRefNode(false, name)));
                            break;
                        default:
                            throw new UnexpectedTokenException(context.Current.Value);
                    }

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
                case TokenType.Var:
                    if (!isClassRoot)
                    {
                        bool isConstant = false;
                        context.MoveNext();

                        if (context.Current?.Type == TokenType.Constant)
                        {
                            isConstant = true;
                            context.MoveNext();
                        }

                        switch (context.Current?.Type)
                        {
                            case TokenType.Bool:
                                context.MoveNext();
                                statements.Add(ProcessDefinition(context, PrivacyType.Local, isConstant,
                                    DefinitionType.Bool));
                                break;
                            case TokenType.String:
                                context.MoveNext();
                                statements.Add(ProcessDefinition(context, PrivacyType.Local, isConstant,
                                    DefinitionType.String));
                                break;
                            case TokenType.Int32:
                                context.MoveNext();
                                statements.Add(ProcessDefinition(context, PrivacyType.Local, isConstant,
                                    DefinitionType.Int32));
                                break;
                            case TokenType.Float32:
                                context.MoveNext();
                                statements.Add(ProcessDefinition(context, PrivacyType.Local, isConstant,
                                    DefinitionType.Float32));
                                break;
                            case TokenType.Name:
                                var classRef = new ClassRefNode(false, context.Current.Value.Text.ToString());
                                context.MoveNext();
                                statements.Add(ProcessDefinition(context, PrivacyType.Local, isConstant,
                                    DefinitionType.ClassObject, classRef));
                                break;
                            default:
                                throw new UnexpectedTokenException(context.Current.Value);
                        }

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
                        statements.Add(new ReturnNode(ProcessExpression(context, null)));
                        break;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }
                case TokenType.Public:
                    if (isClassRoot)
                    {
                        bool isConstant = false;
                        context.MoveNext();

                        if (context.Current?.Type == TokenType.Constant)
                        {
                            isConstant = true;
                            context.MoveNext();
                        }

                        switch (context.Current?.Type)
                        {
                            case TokenType.Bool:
                                context.MoveNext();
                                statements.Add(ProcessDefinition(context, PrivacyType.Public, isConstant,
                                    DefinitionType.Bool));
                                break;
                            case TokenType.String:
                                context.MoveNext();
                                statements.Add(ProcessDefinition(context, PrivacyType.Public, isConstant,
                                    DefinitionType.String));
                                break;
                            case TokenType.Int32:
                                context.MoveNext();
                                statements.Add(ProcessDefinition(context, PrivacyType.Public, isConstant,
                                    DefinitionType.Int32));
                                break;
                            case TokenType.Float32:
                                context.MoveNext();
                                statements.Add(ProcessDefinition(context, PrivacyType.Public, isConstant,
                                    DefinitionType.Float32));
                                break;
                            case TokenType.Void:
                                if (isConstant)
                                {
                                    throw new UnexpectedTokenException(context.Current.Value);
                                }

                                context.MoveNext();
                                statements.Add(ProcessDefinition(context, PrivacyType.Public, false,
                                    DefinitionType.Void));
                                break;
                            case TokenType.Name:
                                var classRef = new ClassRefNode(false, context.Current.Value.Text.ToString());
                                context.MoveNext();
                                statements.Add(ProcessDefinition(context, PrivacyType.Public, isConstant,
                                    DefinitionType.ClassObject, classRef));
                                break;
                            default:
                                throw new UnexpectedTokenException(context.Current.Value);
                        }

                        break;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.Var);
                    }
                case TokenType.Private:
                    if (isClassRoot)
                    {
                        bool isConstant = false;
                        context.MoveNext();

                        if (context.Current?.Type == TokenType.Constant)
                        {
                            isConstant = true;
                            context.MoveNext();
                        }

                        switch (context.Current?.Type)
                        {
                            case TokenType.Bool:
                                context.MoveNext();
                                statements.Add(ProcessDefinition(context, PrivacyType.Private, isConstant,
                                    DefinitionType.Bool));
                                break;
                            case TokenType.String:
                                context.MoveNext();
                                statements.Add(ProcessDefinition(context, PrivacyType.Private, isConstant,
                                    DefinitionType.String));
                                break;
                            case TokenType.Int32:
                                context.MoveNext();
                                statements.Add(ProcessDefinition(context, PrivacyType.Private, isConstant,
                                    DefinitionType.Int32));
                                break;
                            case TokenType.Float32:
                                context.MoveNext();
                                statements.Add(ProcessDefinition(context, PrivacyType.Private, isConstant,
                                    DefinitionType.Float32));
                                break;
                            case TokenType.Void:
                                if (isConstant)
                                {
                                    throw new UnexpectedTokenException(context.Current.Value);
                                }

                                context.MoveNext();
                                statements.Add(ProcessDefinition(context, PrivacyType.Private, false,
                                    DefinitionType.Void));
                                break;
                            case TokenType.Name:
                                var classRef = new ClassRefNode(false, context.Current.Value.Text.ToString());
                                context.MoveNext();
                                statements.Add(ProcessDefinition(context, PrivacyType.Private, isConstant,
                                    DefinitionType.ClassObject, classRef));
                                break;
                            default:
                                throw new UnexpectedTokenException(context.Current.Value);
                        }

                        break;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.Var);
                    }
                case TokenType.This:
                case TokenType.Name:
                    if (!isClassRoot)
                    {
                        statements.Add(ProcessExpression(context, null));
                        break;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }
                case TokenType.Increment:
                    if (!isClassRoot)
                    {
                        ClassRefNode classRef;
                        context.MoveNext();

                        if (context.Current?.Type == TokenType.This)
                        {
                            classRef = new ClassRefNode(true);
                            context.MoveNext();
                        }
                        else if (context.Current?.Type == TokenType.Name)
                        {
                            classRef = new ClassRefNode(false, context.Current.Value.Text.ToString());
                            context.MoveNext();
                        }
                        else
                        {
                            throw new UnexpectedTokenException(context.Current.Value);
                        }

                        if (context.Current?.Type == TokenType.Name)
                        {
                            statements.Add(new IncrementVarNode(new VariableRefNode(context.Current.Value.Text.ToString(), classRef)));
                            break;
                        }
                        else
                        {
                            throw new UnexpectedTokenException(context.Current.Value);
                        }
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }
                case TokenType.Decrement:
                    if (!isClassRoot)
                    {
                        ClassRefNode classRef;
                        context.MoveNext();

                        if (context.Current?.Type == TokenType.This)
                        {
                            classRef = new ClassRefNode(true);
                            context.MoveNext();
                        }
                        else if (context.Current?.Type == TokenType.Name)
                        {
                            classRef = new ClassRefNode(false, context.Current.Value.Text.ToString());
                            context.MoveNext();
                        }
                        else
                        {
                            throw new UnexpectedTokenException(context.Current.Value);
                        }

                        if (context.Current?.Type == TokenType.Name)
                        {
                            statements.Add(new DecrementVarNode(new VariableRefNode(context.Current.Value.Text.ToString(), classRef)));
                            break;
                        }
                        else
                        {
                            throw new UnexpectedTokenException(context.Current.Value);
                        }
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

    public static StatementNode ProcessDefinition(ParseContext context, PrivacyType privacyType, bool isConstant,
        DefinitionType defType, ClassRefNode? classRef = null)
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

        if (context.Current?.Type == TokenType.OpeningParentheses)
        {
            context.MoveNext();
            var @params = ProcessParameterList(context);

            if (context.Current?.Type == TokenType.OpeningCurlyBraces)
            {
                context.MoveNext();
                var statements = ProcessBlock(context);

                return new MethodDefNode(name, privacyType, defType, @params, statements, classRef);
            }
            else
            {
                throw new UnexpectedTokenException(context.Current.Value);
            }
        }
        else if (context.Current?.Type == TokenType.Semicolon)
        {
            context.MoveNext();
            return new FieldNode(name, privacyType, defType, isConstant, classRef);
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value);
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
        switch (context.Current?.Type)
        {
            case TokenType.Bool:
                context.MoveNext();

                if (context.Current?.Type == TokenType.Name)
                {
                    string name = context.Current.Value.Text.ToString();
                    context.MoveNext();
                    return new ParameterNode(DefinitionType.Bool, name, null);
                }
                else
                {
                    throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
                }
            case TokenType.String:
                context.MoveNext();

                if (context.Current?.Type == TokenType.Name)
                {
                    string name = context.Current.Value.Text.ToString();
                    context.MoveNext();
                    return new ParameterNode(DefinitionType.String, name, null);
                }
                else
                {
                    throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
                }
            case TokenType.Int32:
                context.MoveNext();

                if (context.Current?.Type == TokenType.Name)
                {
                    string name = context.Current.Value.Text.ToString();
                    context.MoveNext();
                    return new ParameterNode(DefinitionType.Int32, name, null);
                }
                else
                {
                    throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
                }
            case TokenType.Float32:
                context.MoveNext();

                if (context.Current?.Type == TokenType.Name)
                {
                    string name = context.Current.Value.Text.ToString();
                    context.MoveNext();
                    return new ParameterNode(DefinitionType.Float32, name, null);
                }
                else
                {
                    throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
                }
            case TokenType.Matrix:
                context.MoveNext();

                if (context.Current?.Type == TokenType.Name)
                {
                    string name = context.Current.Value.Text.ToString();
                    context.MoveNext();
                    return new ParameterNode(DefinitionType.Matrix, name, null);
                }
                else
                {
                    throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
                }
            case TokenType.Name:
                var typeName = new ClassRefNode(false, context.Current.Value.Text.ToString());
                context.MoveNext();

                if (context.Current?.Type == TokenType.Name)
                {
                    string name = context.Current.Value.Text.ToString();
                    context.MoveNext();
                    return new ParameterNode(DefinitionType.ClassObject, name, typeName);
                }
                else
                {
                    throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
                }
            default:
                throw new UnexpectedTokenException(context.Current.Value);
        }
    }

    public static IfNode ProcessIf(ParseContext context)
    {
        var condition = ProcessExpression(context, null);
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
            if (context.Current?.Type == TokenType.ClosingParentheses) break;
            args.Add(ProcessExpression(context, null, true));
        }

        context.MoveNext();
        return args;
    }

    // Set lastCreatedNode to null when calling the parent, if not calling parent pass down the variable through all methods.
    // Alternatively, set isParent to true.
    public static ExpressionNode ProcessExpression(ParseContext context, ExpressionNode lastCreatedNode, bool isParameter = false)
    {
        bool isParent = lastCreatedNode == null;

        while (context.Current != null)
        {
            switch (context.Current?.Type)
            {
                case TokenType.OpeningCurlyBraces:
                    if (isParent) context.MoveNext();
                    return lastCreatedNode;
                case TokenType.Semicolon:
                    if (isParent) context.MoveNext();
                    return lastCreatedNode;
                case TokenType.Comma:
                    if (isParent) context.MoveNext();
                    return lastCreatedNode;
                case TokenType.ClosingParentheses:
                    if (!isParameter) context.MoveNext();
                    return lastCreatedNode;
                case TokenType.OpeningParentheses:
                    context.MoveNext();
                    lastCreatedNode = ProcessExpression(context, lastCreatedNode);
                    return lastCreatedNode;
                case TokenType.ClosingSquareBrackets:
                    if (isParent) context.MoveNext();
                    return lastCreatedNode;
                case TokenType.LiteralFloat:
                    lastCreatedNode = new ConstantNode(float.Parse(context.Current.Value.Text.Span));
                    context.MoveNext();
                    break;
                case TokenType.LiteralInt:
                    lastCreatedNode = new ConstantNode(BigInteger.Parse(context.Current.Value.Text.Span));
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
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Addition, lastCreatedNode);
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
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Subtraction, lastCreatedNode);
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
                        lastCreatedNode = ProcessBinaryOp(context, OperationType.Multiplication, lastCreatedNode);
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
                    string name = context.Current.Value.Text.ToString();
                    context.MoveNext();

                    if (context.Current?.Type != TokenType.Period)
                    {
                        lastCreatedNode = new VariableRefNode(name, null, true);
                        break;
                    }

                    context.MoveNext();
                    lastCreatedNode = ProcessAccess(context, new ClassRefNode(false, name));
                    break;
                case TokenType.This:
                    context.MoveNext();

                    if (context.Current?.Type != TokenType.Period)
                    {
                        lastCreatedNode = new ClassRefNode(true);
                        break;
                    }

                    context.MoveNext();
                    lastCreatedNode = ProcessAccess(context, new ClassRefNode(true));
                    break;
                case TokenType.AssignmentSeparator:
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
                    throw new UnexpectedTokenException(context.Current.Value);
            }
        }

        if (lastCreatedNode == null)
        {
            throw new UnexpectedTokenException(context.Current.Value);
        }

        return lastCreatedNode;
    }

    public static MethodCallNode ProcessNew(ParseContext context)
    {
        string name = context.Current.Value.Text.ToString();
        context.MoveNext();

        if (context.Current?.Type != TokenType.OpeningParentheses)
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningParentheses);
        }

        context.MoveNext();
        return new MethodCallNode(name, ProcessArgs(context), new ClassRefNode(false, name));
    }

    public static RefNode ProcessAccess(ParseContext context, ClassRefNode classRefNode)
    {
        RefNode newRefNode = classRefNode;

        while (context.Current != null)
        {
            switch (context.Current?.Type)
            {
                case TokenType.Period:
                    context.MoveNext();
                    break;
                case TokenType.OpeningSquareBrackets:
                    if (context.GetByIndex(context.Position - 1).Type == TokenType.Period)
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    if (newRefNode is ClassRefNode)
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    context.MoveNext();
                    newRefNode = new IndexAccessNode(ProcessExpression(context, null), newRefNode);
                    break;
                case TokenType.Name:
                    if (context.GetByIndex(context.Position - 1).Type != TokenType.Period)
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.Period);
                    }

                    string name = context.Current.Value.Text.ToString();
                    context.MoveNext();

                    switch (context.Current?.Type)
                    {
                        case TokenType.Period:
                            newRefNode = new VariableRefNode(name, newRefNode);
                            break;
                        case TokenType.OpeningParentheses:
                            context.MoveNext();
                            newRefNode = new MethodCallNode(name, ProcessArgs(context), newRefNode);
                            break;
                    }

                    break;
                default:
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
