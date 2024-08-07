﻿using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml;
using Moth.AST.Node;
using Moth.LLVM;
using Moth.LLVM.Data;
using Moth.Tokens;

namespace Moth.AST;

public static class ASTGenerator
{
    public static ScriptAST ProcessScript(ParseContext context)
    {
        NamespaceNode @namespace;
        var contents = new List<IStatementNode>();

        if (context.Current?.Type == TokenType.Namespace)
        {
            context.MoveNext();
            @namespace = ProcessNamespace(context);

            if (context.Current?.Type != TokenType.Semicolon)
                throw new UnexpectedTokenException(context.Current.Value, TokenType.Semicolon);

            context.MoveNext();
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Namespace);
        }

        var attributes = new List<AttributeNode>();

        while (context.Current != null)
        {
            if (context.Current?.Type == TokenType.Import)
            {
                context.MoveNext();
                contents.Add(new ImportNode(ProcessNamespace(context)));

                if (context.Current?.Type != TokenType.Semicolon)
                    throw new UnexpectedTokenException(context.Current.Value, TokenType.Semicolon);

                context.MoveNext();
            }
            else
                break;
        }

        while (context.Current != null)
        {
            switch (context.Current?.Type)
            {
                case TokenType.BlockComment:
                case TokenType.Comment:
                    contents.Add(
                        new CommentNode(
                            context.Current.Value.Text.ToString(),
                            context.Current?.Type == TokenType.BlockComment
                        )
                    );
                    context.MoveNext();
                    break;
                case TokenType.AttributeMarker:
                    attributes.Add(ProcessAttribute(context));
                    break;
                case TokenType.Implement:
                    context.MoveNext();
                    var trait = ProcessTypeRef(context);

                    if (trait.PointerDepth != 0)
                        throw new Exception(
                            $"Cannot implement {trait.ToString()} as it is a pointer."
                        );

                    if (context.Current?.Type != TokenType.For)
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.For);

                    context.MoveNext();
                    var type = ProcessTypeRef(context);

                    if (type.PointerDepth != 0)
                        throw new Exception(
                            $"Cannot implement {type.ToString()} as it is a pointer."
                        );

                    var scope = ProcessScope(context, true);

                    foreach (var statement in scope.Statements)
                    {
                        if (statement is not FuncDefNode fnDef)
                        {
                            throw new Exception(
                                $"Trait implementation contains non-function definition."
                            );
                        }

                        var error =
                            $"Implementation \"{fnDef.Name}\" of trait \"{trait.Name}\" for type \"{type.Name}\"";
                        fnDef.Privacy = PrivacyType.Pub;

                        if (fnDef.IsStatic)
                            throw new Exception($"{error} cannot be static.");

                        if (fnDef.IsForeign)
                            throw new Exception($"{error} cannot be foreign.");

                        if (fnDef.ExecutionBlock == null)
                            throw new Exception($"{error} cannot be unimplemented.");
                    }

                    contents.Add(new ImplementNode(type, trait, scope));
                    break;
                default:
                    var result = ProcessDefinition(context, attributes);
                    attributes = new List<AttributeNode>();
                    contents.Add(result);
                    break;
            }
        }

        return new ScriptAST(@namespace, contents);
    }

    public static NamespaceNode ProcessNamespace(ParseContext context)
    {
        NamespaceNode nmspace = null;
        NamespaceNode lastNmspace = null;

        if (context.Current?.Type == TokenType.Root)
        {
            nmspace = new NamespaceFromRootNode();
            lastNmspace = nmspace;
            context.MoveNext();
        }

        while (context.Current != null)
        {
            switch (context.Current?.Type)
            {
                case TokenType.NamespaceSeparator:
                    context.MoveNext();
                    break;
                case TokenType.Name:
                    if (nmspace == null)
                    {
                        nmspace = new NamespaceNode(context.Current.Value.Text.ToString());
                        lastNmspace = nmspace;
                    }
                    else
                    {
                        lastNmspace.Child = new NamespaceNode(
                            context.Current.Value.Text.ToString()
                        );
                        lastNmspace = lastNmspace.Child;
                    }

                    context.MoveNext();
                    break;
                default:
                    return nmspace;
            }
        }

        throw new UnexpectedTokenException(context.Current.Value);
    }

    public static TraitNode ProcessTraitDef(
        ParseContext context,
        PrivacyType privacy,
        List<AttributeNode> attributes
    )
    {
        if (context.MoveNext()?.Type == TokenType.Name)
        {
            string name = context.Current.Value.Text.ToString();
            context.MoveNext();

            if (context.Current?.Type == TokenType.LesserThan)
            {
                throw new NotImplementedException();
                // var @params = new List<TemplateParameterNode>();
                // context.MoveNext();
                //
                // while (context.Current != null)
                // {
                //     @params.Add(ProcessTemplateParam(context));
                //
                //     if (context.Current?.Type == TokenType.Comma)
                //     {
                //         context.MoveNext();
                //     }
                //     else
                //     {
                //         return context.Current?.Type == TokenType.GreaterThan
                //             ? context.MoveNext()?.Type == TokenType.OpeningCurlyBraces
                //                 ? (TraitNode)new TraitTemplateNode(name, privacy, @params, ProcessScope(context, true), attributes)
                //                 : throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningCurlyBraces)
                //             : throw new UnexpectedTokenException(context.Current.Value);
                //     }
                // }
                //
                // throw new UnexpectedTokenException(context.Current.Value, TokenType.GreaterThan);
            }
            else
            {
                var scope = ProcessScope(context, true);

                foreach (var statement in scope.Statements)
                {
                    if (statement is not FuncDefNode fnDef)
                    {
                        throw new Exception($"Trait definition contains non-function definition.");
                    }

                    var error = $"Definition \"{fnDef.Name}\" of trait \"{name}\"";
                    fnDef.Privacy = PrivacyType.Pub;

                    if (fnDef.IsStatic)
                        throw new Exception($"{error} cannot be static.");

                    if (fnDef.IsForeign)
                        throw new Exception($"{error} cannot be foreign.");

                    if (fnDef.ExecutionBlock != null)
                        throw new Exception($"{error} must be unimplemented.");
                }

                return new TraitNode(name, privacy, scope, attributes);
            }
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
        }
    }

    public static TypeNode ProcessTypeDef(
        ParseContext context,
        PrivacyType privacy,
        bool isUnion,
        List<AttributeNode> attributes
    )
    {
        if (context.MoveNext()?.Type == TokenType.Name)
        {
            string name = context.Current.Value.Text.ToString();
            context.MoveNext();

            if (context.Current?.Type == TokenType.LesserThan)
            {
                var @params = new List<TemplateParameterNode>();
                context.MoveNext();

                while (context.Current != null)
                {
                    @params.Add(ProcessTemplateParam(context));

                    if (context.Current?.Type == TokenType.Comma)
                    {
                        context.MoveNext();
                    }
                    else
                    {
                        return context.Current?.Type == TokenType.GreaterThan
                            ? context.MoveNext()?.Type == TokenType.OpeningCurlyBraces
                                ? (TypeNode)
                                    new TypeTemplateNode(
                                        name,
                                        privacy,
                                        @params,
                                        ProcessScope(context, true),
                                        isUnion,
                                        attributes
                                    )
                                : throw new UnexpectedTokenException(
                                    context.Current.Value,
                                    TokenType.OpeningCurlyBraces
                                )
                            : throw new UnexpectedTokenException(context.Current.Value);
                    }
                }

                throw new UnexpectedTokenException(context.Current.Value, TokenType.GreaterThan);
            }
            else if (context.Current?.Type == TokenType.Semicolon)
            {
                context.MoveNext();
                return new TypeNode(name, privacy, null, isUnion, attributes);
            }
            else
            {
                return new TypeNode(
                    name,
                    privacy,
                    ProcessScope(context, true),
                    isUnion,
                    attributes
                );
            }
        }
        else
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
        }
    }

    private static TemplateParameterNode ProcessTemplateParam(ParseContext context)
    {
        if (context.Current?.Type != TokenType.Name)
        {
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
        }

        string name = context.Current.Value.Text.ToString();

        if (context.MoveNext()?.Type != TokenType.TypeRef)
        {
            return new TemplateParameterNode(name);
        }

        TypeRefNode typeRef = ProcessTypeRef(context);
        return new ConstTemplateParameterNode(name, typeRef);
    }

    public static ScopeNode ProcessScope(ParseContext context, bool isClassRoot = false)
    {
        var statements = new List<IStatementNode>();

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
                    case TokenType.BlockComment:
                    case TokenType.Comment:
                        statements.Add(
                            new CommentNode(
                                context.Current.Value.Text.ToString(),
                                context.Current?.Type == TokenType.BlockComment
                            )
                        );
                        context.MoveNext();
                        break;
                    case TokenType.AttributeMarker:
                        attributes.Add(ProcessAttribute(context));
                        break;
                    default:
                        IStatementNode newDef = ProcessDefinition(context, attributes);
                        attributes = new List<AttributeNode>();
                        statements.Add(newDef);
                        break;
                }
            }
        }
        else
        {
            while (context.Current != null)
            {
                switch (context.Current?.Type)
                {
                    case TokenType.BlockComment:
                    case TokenType.Comment:
                        statements.Add(
                            new CommentNode(
                                context.Current.Value.Text.ToString(),
                                context.Current?.Type == TokenType.BlockComment
                            )
                        );
                        context.MoveNext();
                        break;
                    case TokenType.Return:
                        context.MoveNext();
                        statements.Add(new ReturnNode(ProcessExpression(context, true)));

                        if (context.Current?.Type != TokenType.ClosingCurlyBraces)
                        {
                            throw new UnexpectedTokenException(
                                context.Current.Value,
                                TokenType.ClosingCurlyBraces
                            );
                        }

                        goto case TokenType.ClosingCurlyBraces;
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
                    default:
                        statements.Add(ProcessExpression(context));

                        if (context.Current?.Type == TokenType.Semicolon)
                        {
                            context.MoveNext();
                            break;
                        }
                        else
                        {
                            throw new UnexpectedTokenException(
                                context.Current.Value,
                                TokenType.Semicolon
                            );
                        }
                }
            }
        }

        throw new UnexpectedTokenException(context.Current.Value);
    }

    private static IExpressionNode ProcessIncrementDecrement(ParseContext context)
    {
        var type = (TokenType)(context.Current?.Type);

        context.MoveNext();

        var value = ProcessExpression(context);

        return type == TokenType.Increment
            ? new IncrementVarNode(value)
            : new DecrementVarNode(value);
    }

    public static IStatementNode ProcessWhile(ParseContext context)
    {
        IExpressionNode condition = ProcessExpression(context);

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
                    return new AttributeNode(
                        name,
                        ProcessArgs(context, TokenType.ClosingParentheses)
                    );
                }
                else
                {
                    return new AttributeNode(name, new List<IExpressionNode>());
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

    public static DefinitionNode ProcessDefinition(
        ParseContext context,
        List<AttributeNode>? attributes
    )
    {
        PrivacyType privacy = PrivacyType.Priv;
        bool isForeign = false;
        bool isStatic = false;
        bool isUnion = false;

        while (
            context.Current?.Type == TokenType.Public
            || context.Current?.Type == TokenType.Foreign
            || context.Current?.Type == TokenType.Static
            || context.Current?.Type == TokenType.Union
        )
        {
            switch (context.Current?.Type)
            {
                case TokenType.Public:
                    if (privacy != PrivacyType.Priv)
                        throw new UnexpectedTokenException(context.Current.Value);
                    privacy = PrivacyType.Pub;
                    break;
                case TokenType.Foreign:
                    if (isForeign)
                        throw new UnexpectedTokenException(context.Current.Value);
                    isForeign = true;
                    break;
                case TokenType.Static:
                    if (isStatic)
                        throw new UnexpectedTokenException(context.Current.Value);
                    isStatic = true;
                    break;
                case TokenType.Union:
                    if (isUnion)
                        throw new UnexpectedTokenException(context.Current.Value);
                    isUnion = true;
                    break;
                default:
                    throw new NotImplementedException();
            }

            context.MoveNext();
        }

        if (context.Current?.Type == TokenType.Type)
        {
            return ProcessTypeDef(context, privacy, isUnion, attributes);
        }
        else if (context.Current?.Type == TokenType.Enum)
        {
            return ProcessEnumDef(context, privacy, attributes);
        }
        else if (context.Current?.Type == TokenType.Trait)
        {
            if (isForeign)
                throw new UnexpectedTokenException(context.Current.Value);

            return ProcessTraitDef(context, privacy, attributes);
        }
        else if (context.Current?.Type == TokenType.Function)
        {
            if (context.MoveNext()?.Type == TokenType.Name)
            {
                string name = context.Current.Value.Text.ToString();

                if (context.MoveNext()?.Type == TokenType.OpeningParentheses)
                {
                    context.MoveNext();
                    List<ParameterNode> @params = ProcessParameterList(
                        context,
                        out bool isVariadic
                    );
                    TypeRefNode retTypeRef =
                        context.Current?.Type == TokenType.OpeningCurlyBraces
                        || context.Current?.Type == TokenType.Semicolon
                            ? new TypeRefNode(Reserved.Void, 0, false)
                            : ProcessTypeRef(context);
                    ScopeNode scope;

                    if (context.Current?.Type == TokenType.Semicolon)
                    {
                        context.MoveNext();
                        scope = null;
                    }
                    else
                    {
                        scope = ProcessScope(context);
                    }

                    return new FuncDefNode(
                        name,
                        privacy,
                        retTypeRef,
                        @params,
                        scope,
                        isVariadic,
                        isStatic,
                        isForeign,
                        attributes
                    );
                }
                else
                {
                    throw new UnexpectedTokenException(
                        context.Current.Value,
                        TokenType.OpeningParentheses
                    );
                }
            }
            else
            {
                throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
            }
        }
        else if (context.Current?.Type == TokenType.Global)
        {
            if (context.MoveNext()?.Type == TokenType.Name)
            {
                string name = context.Current.Value.Text.ToString();
                context.MoveNext();
                TypeRefNode typeRef = ProcessTypeRef(context);

                if (context.Current?.Type == TokenType.Semicolon)
                {
                    context.MoveNext();
                    return new GlobalVarNode(
                        name,
                        typeRef,
                        privacy,
                        false /*TODO*/
                        ,
                        isForeign,
                        attributes
                    );
                }
                else
                {
                    throw new UnexpectedTokenException(context.Current.Value, TokenType.Semicolon);
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

    public static EnumNode ProcessEnumDef(
        ParseContext context,
        PrivacyType privacy,
        List<AttributeNode> attributes
    )
    {
        if (context.Current?.Type != TokenType.Enum)
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Enum);

        if (context.MoveNext()?.Type != TokenType.Name)
            throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);

        string name = context.Current.Value.Text.ToString();

        if (context.MoveNext()?.Type != TokenType.OpeningCurlyBraces)
            throw new UnexpectedTokenException(context.Current.Value, TokenType.OpeningCurlyBraces);

        var enumFlags = new List<EnumFlagNode>();
        ulong index = 0;

        while (context.Current != null)
        {
            if (context.MoveNext()?.Type != TokenType.Name)
                break;

            string entryName = context.Current.Value.Text.ToString();

            if (context.MoveNext()?.Type == TokenType.Assign)
                if (context.MoveNext()?.Type == TokenType.LiteralInt)
                {
                    index = UInt64.Parse(context.Current.Value.Text.ToString());
                    context.MoveNext();
                }
                else
                    throw new UnexpectedTokenException(context.Current.Value, TokenType.LiteralInt);

            enumFlags.Add(new EnumFlagNode(entryName, index));
            index++;

            if (context.Current?.Type != TokenType.Comma)
                break;
        }

        if (context.Current?.Type != TokenType.ClosingCurlyBraces)
            throw new UnexpectedTokenException(context.Current.Value, TokenType.ClosingCurlyBraces);

        if (context.MoveNext()?.Type != TokenType.Extend)
            return new EnumNode(name, privacy, enumFlags, null, attributes);

        context.MoveNext();
        return new EnumNode(name, privacy, enumFlags, ProcessScope(context, true), attributes);
    }

    public static List<ParameterNode> ProcessParameterList(
        ParseContext context,
        out bool isVariadic
    )
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
                            throw new UnexpectedTokenException(
                                context.Current.Value,
                                TokenType.ClosingParentheses
                            );
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
        IExpressionNode condition = ProcessExpression(context);

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
                return new ScopeNode(
                    new List<IStatementNode>
                    {
                        ProcessIf(context) //TODO: this does not work in compilation
                    }
                );
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

    public static List<IExpressionNode> ProcessArgs(ParseContext context, TokenType terminator)
    {
        var args = new List<IExpressionNode>();

        while (context.Current != null)
        {
            if (context.Current?.Type == terminator)
            {
                context.MoveNext();
                break;
            }

            args.Add(ProcessExpression(context));

            if (context.Current?.Type == terminator)
            {
                context.MoveNext();
                break;
            }
            else if (context.Current?.Type != TokenType.Comma)
            {
                throw new UnexpectedTokenException(context.Current.Value, terminator);
            }

            context.MoveNext();
        }

        return args;
    }

    public static TypeRefNode ProcessTypeRef(ParseContext context)
    {
        if (context.Current?.Type == TokenType.TemplateTypeRef)
        {
            if (context.MoveNext()?.Type != TokenType.Name)
            {
                throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
            }

            string retTypeName = context.Current.Value.Text.ToString();
            uint pointerDepth = 0;
            bool isRef = false;

            context.MoveNext();

            while (
                context.Current?.Type == TokenType.Asterix
                || context.Current?.Type == TokenType.Ampersand
            )
            {
                if (context.Current?.Type == TokenType.Ampersand)
                {
                    isRef = true;
                    context.MoveNext();
                    break;
                }

                pointerDepth++;
                context.MoveNext();
            }

            return new LocalTypeRefNode(retTypeName, pointerDepth, isRef);
        }
        else
        {
            NamespaceNode nmspace = null;
            bool b = false;

            if (context.Current?.Type != TokenType.TypeRef)
            {
                nmspace = ProcessNamespace(context);
                b = true;
            }

            if (context.Current?.Type != TokenType.TypeRef)
                throw new UnexpectedTokenException(context.Current.Value, TokenType.TypeRef);

            if (context.MoveNext()?.Type == TokenType.Name)
            {
                string retTypeName = context.Current.Value.Text.ToString();
                var genericParams = new List<IExpressionNode>();
                uint pointerDepth = 0;
                bool isRef = false;

                if (context.MoveNext()?.Type == TokenType.LesserThan)
                {
                    context.MoveNext();

                    while (context.Current?.Type != TokenType.GreaterThan)
                    {
                        if (context.Current?.Type is TokenType.TypeRef or TokenType.TemplateTypeRef)
                        {
                            genericParams.Add(ProcessTypeRef(context));
                        }
                        else if (context.Current?.Type == TokenType.OpeningParentheses)
                        {
                            context.MoveNext();
                            genericParams.Add(ProcessExpression(context));

                            if (context.Current?.Type != TokenType.ClosingParentheses)
                            {
                                throw new UnexpectedTokenException(
                                    context.Current.Value,
                                    TokenType.ClosingParentheses
                                );
                            }

                            context.MoveNext();
                        }
                        else
                        {
                            throw new UnexpectedTokenException(context.Current.Value);
                        }

                        if (context.Current?.Type == TokenType.Comma)
                        {
                            context.MoveNext();
                        }
                        else if (context.Current?.Type != TokenType.GreaterThan)
                        {
                            throw new UnexpectedTokenException(context.Current.Value);
                        }
                    }

                    context.MoveNext();
                }

                while (
                    context.Current?.Type == TokenType.Asterix
                    || context.Current?.Type == TokenType.Ampersand
                )
                {
                    if (context.Current?.Type == TokenType.Ampersand)
                    {
                        isRef = true;
                        context.MoveNext();
                        break;
                    }

                    pointerDepth++;
                    context.MoveNext();
                }

                var retVal =
                    genericParams.Count != 0
                        ? new TemplateTypeRefNode(retTypeName, genericParams, pointerDepth, isRef)
                        : new TypeRefNode(retTypeName, pointerDepth, isRef);
                if (b)
                    retVal.Namespace = nmspace;
                return retVal;
            }
            else if (!b && context.Current?.Type == TokenType.OpeningParentheses)
            {
                var @params = new List<TypeRefNode>();
                uint pointerDepth = 0;
                bool isRef = false;
                TypeRefNode retType;

                while (context.MoveNext()?.Type is TokenType.TypeRef or TokenType.TemplateTypeRef)
                {
                    @params.Add(ProcessTypeRef(context));

                    if (context.Current?.Type == TokenType.ClosingParentheses)
                    {
                        break;
                    }
                    else if (context.Current?.Type != TokenType.Comma)
                    {
                        throw new UnexpectedTokenException(
                            context.Current.Value,
                            TokenType.ClosingParentheses
                        );
                    }
                }

                context.MoveNext();

                while (
                    context.Current?.Type == TokenType.Asterix
                    || context.Current?.Type == TokenType.Ampersand
                )
                {
                    if (context.Current?.Type == TokenType.Ampersand)
                    {
                        isRef = true;
                        context.MoveNext();
                        break;
                    }

                    pointerDepth++;
                    context.MoveNext();
                }

                if (context.Current?.Type == TokenType.TypeRef)
                {
                    retType = ProcessTypeRef(context);
                    return new FuncTypeRefNode(retType, @params, pointerDepth, isRef);
                }
                else
                {
                    throw new UnexpectedTokenException(context.Current.Value, TokenType.TypeRef);
                }
            }
            else if (!b && context.Current?.Type == TokenType.OpeningSquareBrackets)
            {
                uint pointerDepth = 0;
                bool isRef = false;

                if (!(context.MoveNext()?.Type is TokenType.TypeRef or TokenType.TemplateTypeRef))
                {
                    throw new UnexpectedTokenException(context.Current.Value);
                }

                var elementType = ProcessTypeRef(context);

                if (context.Current?.Type != TokenType.ClosingSquareBrackets)
                {
                    throw new UnexpectedTokenException(
                        context.Current.Value,
                        TokenType.ClosingSquareBrackets
                    );
                }

                context.MoveNext();

                while (
                    context.Current?.Type == TokenType.Asterix
                    || context.Current?.Type == TokenType.Ampersand
                )
                {
                    if (context.Current?.Type == TokenType.Ampersand)
                    {
                        isRef = true;
                        context.MoveNext();
                        break;
                    }

                    pointerDepth++;
                    context.MoveNext();
                }

                return new ArrayTypeRefNode(elementType, pointerDepth, isRef);
            }
            else
            {
                throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
            }
        }
    }

    public static IExpressionNode ProcessExpression(ParseContext context, bool nullAllowed = false)
    {
        Stack<IExpressionNode> stack = new Stack<IExpressionNode>();

        while (context.Current != null)
        {
            switch (context.Current?.Type)
            {
                case TokenType.OpeningParentheses:
                {
                    context.MoveNext();
                    var newNode = new SubExprNode(ProcessExpression(context));

                    if (context.Current?.Type != TokenType.ClosingParentheses)
                    {
                        throw new UnexpectedTokenException(
                            context.Current.Value,
                            TokenType.ClosingParentheses
                        );
                    }

                    if (stack.Count > 0 && stack.Peek() is TypeRefNode typeRef)
                    {
                        stack.Pop();
                        stack.Push(new CastNode(typeRef, newNode));
                    }
                    else
                    {
                        stack.Push(newNode);
                    }

                    context.MoveNext();
                    break;
                }
                case TokenType.OpeningSquareBrackets:
                {
                    if (stack.Count == 0)
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    if (stack.Peek() is TypeRefNode typeRef)
                    {
                        stack.Pop();
                        stack.Push(ProcessLiteralArray(context, typeRef));
                    }
                    else
                    {
                        context.MoveNext();
                        stack.Push(
                            new IndexAccessNode(
                                stack.Pop(),
                                ProcessArgs(context, TokenType.ClosingSquareBrackets)
                            )
                        );
                    }

                    break;
                }
                case TokenType.LiteralFloat:
                    stack.Push(new LiteralNode(float.Parse(context.Current.Value.Text.Span)));
                    context.MoveNext();
                    break;
                case TokenType.LiteralInt:
                    stack.Push(new LiteralNode(int.Parse(context.Current.Value.Text.Span)));
                    context.MoveNext();
                    break;
                case TokenType.LiteralString:
                    stack.Push(new LiteralNode(context.Current.Value.Text.ToString()));
                    context.MoveNext();
                    break;
                case TokenType.LiteralChar:
                    stack.Push(new LiteralNode(context.Current.Value.Text.ToString()[0]));
                    context.MoveNext();
                    break;
                case TokenType.True:
                    stack.Push(new LiteralNode(true));
                    context.MoveNext();
                    break;
                case TokenType.False:
                    stack.Push(new LiteralNode(false));
                    context.MoveNext();
                    break;
                case TokenType.Null:
                    stack.Push(new LiteralNode(null));
                    context.MoveNext();
                    break;
                case TokenType.Ampersand:
                    context.MoveNext();
                    stack.Push(new RefOfNode(ProcessExpression(context)));
                    break;
                case TokenType.Function:
                    if (context.MoveNext()?.Type == TokenType.OpeningParentheses)
                    {
                        context.MoveNext();
                        List<ParameterNode> @params = ProcessParameterList(
                            context,
                            out bool isVariadic
                        );
                        TypeRefNode retType = ProcessTypeRef(context);

                        if (isVariadic)
                        {
                            throw new Exception(
                                $"{new UnexpectedTokenException(context.Previous().Value).Message}"
                                    + $"\nCannot have a variadic locally-defined function."
                            );
                        }

                        if (context.Current?.Type == TokenType.OpeningCurlyBraces)
                        {
                            ScopeNode scope = ProcessScope(context);
                            stack.Push(new LocalFuncDefNode(retType, @params, scope));
                        }
                        else
                        {
                            throw new UnexpectedTokenException(
                                context.Current.Value,
                                TokenType.OpeningCurlyBraces
                            );
                        }
                    }
                    else
                    {
                        throw new UnexpectedTokenException(
                            context.Current.Value,
                            TokenType.OpeningParentheses
                        );
                    }

                    break;
                case TokenType.Root:
                    context.MoveNext();

                    var nmspace = ProcessNamespace(context);

                    if (context.Current?.Type == TokenType.TypeRef)
                    {
                        var typeRef = ProcessTypeRef(context);
                        typeRef.Namespace = nmspace;
                        stack.Push(typeRef);
                    }
                    else
                    {
                        stack.Push(nmspace);
                    }

                    break;
                case TokenType.Period:
                    if (stack.Count == 0)
                        throw new UnexpectedTokenException(context.Current.Value);

                    if (context.MoveNext()?.Type == TokenType.Name)
                    {
                        string name = context.Current.Value.Text.ToString();

                        if (context.MoveNext()?.Type == TokenType.OpeningParentheses)
                        {
                            context.MoveNext();
                            stack.Push(
                                new FuncCallNode(
                                    name,
                                    ProcessArgs(context, TokenType.ClosingParentheses),
                                    stack.Pop()
                                )
                            );
                        }
                        else
                        {
                            stack.Push(new RefNode(name, stack.Pop()));
                        }

                        break;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
                    }
                case TokenType.Local:
                    if (context.MoveNext()?.Type == TokenType.Name)
                    {
                        string name = context.Current.Value.Text.ToString();

                        if (context.MoveNext()?.Type == TokenType.Assign)
                        {
                            context.MoveNext();
                            stack.Push(new InferredLocalDefNode(name, ProcessExpression(context)));
                        }
                        else
                        {
                            TypeRefNode type = ProcessTypeRef(context);
                            stack.Push(new LocalDefNode(name, type));
                        }
                    }
                    else
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.Name);
                    }

                    break;
                case TokenType.If:
                    context.MoveNext();
                    IExpressionNode condition = ProcessExpression(context);

                    if (context.Current?.Type != TokenType.Then)
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.Then);
                    }

                    context.MoveNext();
                    IExpressionNode then = ProcessExpression(context);

                    if (context.Current?.Type != TokenType.Else)
                    {
                        throw new UnexpectedTokenException(context.Current.Value, TokenType.Else);
                    }

                    context.MoveNext();
                    IExpressionNode @else = ProcessExpression(context);

                    stack.Push(new InlineIfNode(condition, then, @else));
                    break;
                case TokenType.Hyphen:
                {
                    if (stack.Count != 0 && stack.Peek() is not BinaryOperationNode)
                    {
                        goto case TokenType.Assign;
                    }

                    var newNode = new BinaryOperationNode(
                        new LiteralNode(-1),
                        OperationType.Multiplication
                    );

                    if (stack.Count != 0 && stack.Peek() is BinaryOperationNode lastBinOp)
                    {
                        lastBinOp.Right = newNode;
                    }

                    context.MoveNext();
                    stack.Push(newNode);
                    break;
                }
                case TokenType.Asterix:
                    if (stack.Count != 0 && stack.Peek() is not BinaryOperationNode)
                    {
                        goto case TokenType.Assign;
                    }

                    context.MoveNext();
                    stack.Push(new DeRefNode(ProcessExpression(context)));
                    break;
                case TokenType.Assign:
                case TokenType.Plus:
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

                    if (stack.Count == 0)
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    var contestedExpr = stack.Pop();

                    if (stack.Count > 0 && stack.Peek() is BinaryOperationNode lastBinOp)
                    {
                        if (GetOpPriority(opType) > GetOpPriority(lastBinOp.Type))
                        {
                            var newNode = new BinaryOperationNode(contestedExpr, opType);
                            lastBinOp.Right = newNode;
                            stack.Push(newNode);
                        }
                        else
                        {
                            lastBinOp.Right = contestedExpr;
                            stack.Pop();

                            var newNode = new BinaryOperationNode(lastBinOp, opType);

                            if (stack.Count > 0 && stack.Peek() is BinaryOperationNode parent)
                            {
                                parent.Right = newNode;
                            }

                            stack.Push(newNode);
                        }
                    }
                    else
                    {
                        stack.Push(new BinaryOperationNode(contestedExpr, opType));
                    }

                    context.MoveNext();
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

                    if (stack.Count == 0)
                    {
                        throw new UnexpectedTokenException(context.Current.Value);
                    }

                    var newNode = new BinaryOperationNode(stack.Peek(), OperationType.Assignment);
                    var newNode2 = new BinaryOperationNode(stack.Peek(), opType);

                    context.MoveNext();
                    newNode.Right = newNode2;
                    newNode2.Right = ProcessExpression(context);
                    stack.Pop();
                    stack.Push(newNode);
                    break;
                }
                // case TokenType.ScientificNotation:
                //     context.MoveNext();
                //     lastCreatedNode = new BinaryOperationNode(lastCreatedNode,
                //         ProcessBinaryOp(context, OperationType.Exponential, new LiteralNode(10)),
                //         OperationType.Multiplication);
                //     break;
                case TokenType.Not:
                    if (stack.Count > 0 && stack.Peek() is not BinaryOperationNode)
                        throw new UnexpectedTokenException(context.Current.Value);

                    context.MoveNext();
                    stack.Push(new InverseNode(ProcessExpression(context)));
                    break;
                case TokenType.Increment:
                case TokenType.Decrement:
                    if (stack.Count > 0 && stack.Peek() is not BinaryOperationNode)
                        throw new UnexpectedTokenException(context.Current.Value);

                    stack.Push(ProcessIncrementDecrement(context));
                    break;
                case TokenType.TemplateTypeRef:
                case TokenType.TypeRef:
                    if (stack.Count > 0 && stack.Peek() is not BinaryOperationNode)
                        throw new UnexpectedTokenException(context.Current.Value);

                    stack.Push(ProcessTypeRef(context));
                    break;
                case TokenType.Name:
                {
                    string name = context.Current.Value.Text.ToString();

                    if (context.MoveNext()?.Type == TokenType.OpeningParentheses)
                    {
                        context.MoveNext();
                        stack.Push(
                            new FuncCallNode(
                                name,
                                ProcessArgs(context, TokenType.ClosingParentheses),
                                null
                            )
                        );
                    }
                    else
                    {
                        stack.Push(new RefNode(name, null));
                    }

                    break;
                }
                case TokenType.This:
                    if (stack.Count > 0 && stack.Peek() is not BinaryOperationNode)
                        throw new UnexpectedTokenException(context.Current.Value);

                    stack.Push(new SelfNode());
                    context.MoveNext();
                    break;
                default:
                    if (nullAllowed && stack.Count == 0)
                    {
                        return null;
                    }

                    if (stack.Peek() is not BinaryOperationNode)
                    {
                        var latest = stack.Pop();

                        if (stack.Count > 0 && stack.Peek() is BinaryOperationNode binOp)
                        {
                            binOp.Right = latest;
                        }
                        else
                        {
                            stack.Push(latest);
                        }
                    }

                    return stack.Last();
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
            TokenType.Modulo => OperationType.Modulus,
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
            TokenType.ModAssign => OperationType.Modulus,
            TokenType.ExpAssign => OperationType.Exponential,
            _ => throw new UnexpectedTokenException(context.Current.Value)
        };
    }

    public static LiteralArrayNode ProcessLiteralArray(
        ParseContext context,
        TypeRefNode elementType
    )
    {
        var elements = new List<IExpressionNode>();

        if (context.Current?.Type != TokenType.OpeningSquareBrackets)
        {
            throw new UnexpectedTokenException(
                context.Current.Value,
                TokenType.OpeningSquareBrackets
            );
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
                    elements.Add(ProcessExpression(context));
                    break;
            }
        }

        if (context.Current?.Type != TokenType.ClosingSquareBrackets)
        {
            throw new UnexpectedTokenException(
                context.Current.Value,
                TokenType.ClosingSquareBrackets
            );
        }

        context.MoveNext();
        return new LiteralArrayNode(elementType, elements.ToArray());
    }

    private static int GetOpPriority(OperationType operationType)
    {
        return operationType switch
        {
            OperationType.Exponential => 5,
            OperationType.Modulus or OperationType.Multiplication or OperationType.Division => 4,
            OperationType.Addition or OperationType.Subtraction => 3,
            OperationType.Equal
            or OperationType.NotEqual
            or OperationType.LesserThanOrEqual
            or OperationType.GreaterThanOrEqual
            or OperationType.LesserThan
            or OperationType.GreaterThan
                => 2,
            OperationType.Or or OperationType.And => 1,
            _ => 0,
        };
    }
}
