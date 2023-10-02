using LLVMSharp.Interop;
using Moth.AST;
using Moth.AST.Node;
using Moth.LLVM.Data;
using Moth.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public static class LLVMCompiler
{
    public static void Compile(CompilerContext compiler, ScriptAST[] scripts)
    {
        foreach (var script in scripts)
        {
            foreach (var @class in script.ClassNodes)
            {
                DefineClass(compiler, @class);
            }
        }

        foreach (var script in scripts)
        {
            foreach (var classNode in script.ClassNodes)
            {
                if (compiler.Classes.TryGetValue(classNode.Name, out Class @class))
                {
                    foreach (var funcDefNode in classNode.Scope.Statements.OfType<FuncDefNode>())
                    {
                        DefineFunction(compiler, funcDefNode, @class);
                    }
                }
                else
                {
                    throw new Exception("Unnamed critical error. Report ASAP.");
                }
            }

            foreach (var funcDefNode in script.GlobalFunctions)
            {
                DefineFunction(compiler, funcDefNode);
            }
        }

        foreach (var script in scripts)
        {
            foreach (var @class in script.ClassNodes)
            {
                CompileClass(compiler, @class);
            }
        }

        foreach (var script in scripts)
        {
            foreach (var classNode in script.ClassNodes)
            {
                if (compiler.Classes.TryGetValue(classNode.Name, out Class @class))
                {
                    foreach (var funcDefNode in classNode.Scope.Statements.OfType<FuncDefNode>())
                    {
                        CompileFunction(compiler, funcDefNode, @class);
                    }
                }
                else
                {
                    throw new Exception("Unnamed critical error. Report ASAP.");
                }
            }

            foreach (var funcDefNode in script.GlobalFunctions)
            {
                CompileFunction(compiler, funcDefNode);
            }
        }
    }

    public static void DefineClass(CompilerContext compiler, ClassNode classNode)
    {
        LLVMTypeRef newStruct = compiler.Context.CreateNamedStruct(classNode.Name);
        Class newClass = new Class(compiler, classNode.Name, newStruct, classNode.Privacy);
        compiler.Classes.Add(classNode.Name, newClass);
        newClass.AddBuiltins(compiler);
    }

    public static void CompileClass(CompilerContext compiler, ClassNode classNode)
    {
        uint index = 0;
        List<LLVMTypeRef> lLVMTypes = new List<LLVMTypeRef>();

        if (compiler.Classes.TryGetValue(classNode.Name, out Class @class))
        {
            foreach (FieldDefNode field in classNode.Scope.Statements.OfType<FieldDefNode>())
            {
                if (compiler.Classes.TryGetValue(field.TypeRef.Name, out Class type))
                {
                    LLVMTypeRef fieldLLVMType;

                    if (field.TypeRef.IsPointer)
                    {
                        fieldLLVMType = LLVMTypeRef.CreatePointer(type.LLVMType, 0);
                    }
                    else
                    {
                        fieldLLVMType = type.LLVMType;
                    }

                    lLVMTypes.Add(fieldLLVMType);
                    @class.Fields.Add(field.Name, new Field(field.Name, index, fieldLLVMType, field.Privacy, type));
                    index++;
                }
            }

            @class.LLVMType.StructSetBody(lLVMTypes.ToArray(), false);
        }
        else
        {
            throw new Exception("Tried to get a class that doesn't exist. This is a critical error that must be reported.");
        }
    }

    public static void DefineFunction(CompilerContext compiler, FuncDefNode funcDefNode, Class @class = null)
    {
        int index = 1;
        List<Parameter> @params = new List<Parameter>();
        List<LLVMTypeRef> paramTypes = new List<LLVMTypeRef>();

        if (@class != null && funcDefNode.Privacy != PrivacyType.Static)
        {
            paramTypes.Add(LLVMTypeRef.CreatePointer(@class.LLVMType, 0));
        }

        foreach (ParameterNode paramNode in funcDefNode.Params)
        {
            if (compiler.Classes.TryGetValue(paramNode.TypeRef.Name, out Class type))
            {
                LLVMTypeRef paramLLVMType;

                if (paramNode.TypeRef.IsPointer)
                {
                    paramLLVMType = LLVMTypeRef.CreatePointer(type.LLVMType, 0);
                }
                else
                {
                    paramLLVMType = type.LLVMType;
                }

                @params.Add(new Parameter(index, paramNode.Name, paramLLVMType, type));
                paramTypes.Add(paramLLVMType);
                index++;
            }
            else
            {
                throw new Exception($"Type {paramNode.TypeRef.Name} does not exist.");
            }
        }

        if (compiler.Classes.TryGetValue(funcDefNode.ReturnTypeRef.Name, out Class classOfReturnType))
        {
            LLVMTypeRef returnLLVMType;

            if (funcDefNode.ReturnTypeRef.IsPointer)
            {
                returnLLVMType = LLVMTypeRef.CreatePointer(classOfReturnType.LLVMType, 0);
            }
            else
            {
                returnLLVMType = classOfReturnType.LLVMType;
            }

            LLVMTypeRef lLVMFuncType = LLVMTypeRef.CreateFunction(returnLLVMType, paramTypes.ToArray(), funcDefNode.IsVariadic);
            LLVMValueRef lLVMFunc = compiler.Module.AddFunction(funcDefNode.Name, lLVMFuncType);
            Function func = new Function(lLVMFunc,
                lLVMFuncType,
                classOfReturnType.LLVMType,
                funcDefNode.Privacy,
                classOfReturnType,
                @class,
                @params,
                funcDefNode.IsVariadic);

            if (@class != null)
            {
                if (func.Privacy == PrivacyType.Static)
                {
                    @class.StaticMethods.Add(funcDefNode.Name, func);
                }
                else
                {
                    @class.Methods.Add(funcDefNode.Name, func);
                }
            }
            else
            {
                compiler.GlobalFunctions.Add(funcDefNode.Name, func);
            }
        }
    }

    public static void CompileFunction(CompilerContext compiler, FuncDefNode funcDefNode, Class @class = null)
    {
        Function func;

        if (funcDefNode.Privacy == PrivacyType.Foreign && funcDefNode.ExecutionBlock == null)
        {
            return;
        }
        else if (@class != null && funcDefNode.Privacy != PrivacyType.Static && @class.Methods.TryGetValue(funcDefNode.Name, out func))
        {
            // Keep empty
        }
        else if (@class != null && funcDefNode.Privacy == PrivacyType.Static && @class.StaticMethods.TryGetValue(funcDefNode.Name, out func))
        {
            // Keep empty
        }
        else if (compiler.GlobalFunctions.TryGetValue(funcDefNode.Name, out func))
        {
            // Keep empty
        }
        else
        {
            throw new Exception($"Cannot compile function {funcDefNode.Name} as it is undefined.");
        }

        func.OpeningScope = new Scope(func.LLVMFunc.AppendBasicBlock("entry"));
        compiler.Builder.PositionAtEnd(func.OpeningScope.LLVMBlock);
        compiler.CurrentFunction = func;

        foreach (Parameter param in compiler.CurrentFunction.Params)
        {
            var paramAsVar = compiler.Builder.BuildAlloca(param.LLVMType, param.Name);
            compiler.Builder.BuildStore(func.LLVMFunc.Params[param.ParamIndex], paramAsVar);
            func.OpeningScope.Variables.Add(param.Name,
                new Variable(paramAsVar,
                    param.LLVMType,
                    PrivacyType.Local,
                    param.ClassOfType));
        }

        if (!CompileScope(compiler, func.OpeningScope, funcDefNode.ExecutionBlock))
        {
            throw new Exception("Function is not guaranteed to return.");
        }
    }

    public static bool CompileScope(CompilerContext compiler, Scope scope, ScopeNode scopeNode)
    {
        compiler.Builder.PositionAtEnd(scope.LLVMBlock);

        foreach (StatementNode statement in scopeNode.Statements)
        {
            if (statement is ReturnNode @return)
            {
                if (@return.ReturnValue != null)
                {
                    compiler.Builder.BuildRet(CompileExpression(compiler, scope, @return.ReturnValue).LLVMValue);
                }
                else
                {
                    compiler.Builder.BuildRetVoid();
                }

                return true;
            }
            else if (statement is FieldDefNode fieldDef)
            {
                if (fieldDef is InferredLocalDefNode inferredLocalDef)
                {
                    var defaultVal = CompileExpression(compiler, scope, inferredLocalDef.DefaultValue);
                    var lLVMVar = compiler.Builder.BuildAlloca(defaultVal.LLVMType, fieldDef.Name);
                    compiler.Builder.BuildStore(defaultVal.LLVMValue, lLVMVar);
                    scope.Variables.Add(fieldDef.Name,
                        new Variable(lLVMVar,
                            defaultVal.LLVMType,
                            fieldDef.Privacy,
                            defaultVal.ClassOfType));
                }
                else if (compiler.Classes.TryGetValue(fieldDef.TypeRef.Name, out Class type))
                {
                    LLVMTypeRef varLLVMType;

                    if (fieldDef.TypeRef.IsPointer)
                    {
                        varLLVMType = LLVMTypeRef.CreatePointer(type.LLVMType, 0);
                    }
                    else
                    {
                        varLLVMType = type.LLVMType;
                    }

                    var lLVMVar = compiler.Builder.BuildAlloca(varLLVMType, fieldDef.Name);
                    scope.Variables.Add(fieldDef.Name,
                        new Variable(lLVMVar,
                            varLLVMType,
                            fieldDef.Privacy,
                            type));

                    if (fieldDef is LocalDefNode localDef)
                    {
                        compiler.Builder.BuildStore(CompileExpression(compiler, scope, localDef.DefaultValue).LLVMValue, lLVMVar);
                    }
                }
                else
                {
                    throw new Exception("Failure at: \"Moth.LLVM.LLVMCompiler#CompileScope(CompilerContext, Scope, ScopeNode)\"");
                }
            }
            else if (statement is ScopeNode newScopeNode)
            {
                var newScope = new Scope(compiler.CurrentFunction.LLVMFunc.AppendBasicBlock(""));
                newScope.Variables = scope.Variables; //TODO: fix it? maybe?
                compiler.Builder.BuildBr(newScope.LLVMBlock);
                compiler.Builder.PositionAtEnd(newScope.LLVMBlock);
                
                if (CompileScope(compiler, newScope, newScopeNode))
                {
                    return true;
                }

                scope.LLVMBlock = compiler.CurrentFunction.LLVMFunc.AppendBasicBlock("");
                compiler.Builder.BuildBr(scope.LLVMBlock);
                compiler.Builder.PositionAtEnd(scope.LLVMBlock);
            }
            else if (statement is IfNode @if)
            {
                var condition = CompileExpression(compiler, scope, @if.Condition);
                var then = compiler.CurrentFunction.LLVMFunc.AppendBasicBlock("then");
                var @else = compiler.CurrentFunction.LLVMFunc.AppendBasicBlock("else");
                LLVMBasicBlockRef @continue = null;
                bool thenReturned = false;
                bool elseReturned = false;

                compiler.Builder.BuildCondBr(condition.LLVMValue, then, @else);

                //then
                {
                    compiler.Builder.PositionAtEnd(then);

                    {
                        var newScope = new Scope(then);
                        newScope.Variables = scope.Variables; //TODO: fix it? maybe?
                        
                        if (CompileScope(compiler, newScope, @if.Then))
                        {
                            thenReturned = true;
                        }
                        else
                        {
                            if (@continue == null)
                            {
                                @continue = compiler.CurrentFunction.LLVMFunc.AppendBasicBlock("continue");
                            }

                            compiler.Builder.BuildBr(@continue);
                        }
                    }
                }

                //else
                {
                    compiler.Builder.PositionAtEnd(@else);

                    if (@if.Else != null)
                    {
                        var newScope = new Scope(@else);
                        newScope.Variables = scope.Variables; //TODO: fix it? maybe?
                        
                        if (CompileScope(compiler, newScope, @if.Else))
                        {
                            elseReturned = true;
                        }
                        else
                        {
                            if (@continue == null)
                            {
                                @continue = compiler.CurrentFunction.LLVMFunc.AppendBasicBlock("continue");
                            }

                            compiler.Builder.BuildBr(@continue);
                        }
                    }
                }

                if (thenReturned && elseReturned)
                {
                    return true;
                }
                else
                {
                    compiler.Builder.PositionAtEnd(@continue);
                    scope.LLVMBlock = @continue;
                }
            }
            else if (statement is ExpressionNode exprNode)
            {
                CompileExpression(compiler, scope, exprNode);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        return false;
    }

    public static ValueContext CompileExpression(CompilerContext compiler, Scope scope, ExpressionNode expr)
    {
        if (expr is BinaryOperationNode binaryOp)
        {
            if (binaryOp.Type == OperationType.Assignment)
            {
                if (binaryOp.Left is RefNode @ref)
                {
                    var variableAssigned = CompileRef(compiler, scope, @ref);
                    compiler.Builder.BuildStore(CompileExpression(compiler, scope, binaryOp.Right).LLVMValue, variableAssigned.LLVMValue);
                    return new ValueContext(variableAssigned.LLVMType, variableAssigned.LLVMValue, variableAssigned.ClassOfType);
                }
                else
                {
                    throw new Exception();
                }
            }
            else
            {
                var left = CompileExpression(compiler, scope, binaryOp.Left);
                var right = CompileExpression(compiler, scope, binaryOp.Right);

                if (left.ClassOfType == right.ClassOfType)
                {
                    LLVMValueRef leftVal;
                    LLVMValueRef rightVal;
                    LLVMValueRef builtVal;

                    if (left.LLVMValue.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
                    {
                        leftVal = compiler.Builder.BuildLoad2(left.LLVMType, left.LLVMValue);
                    }
                    else
                    {
                        leftVal = left.LLVMValue;
                    }

                    if (right.LLVMValue.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
                    {
                        rightVal = compiler.Builder.BuildLoad2(right.LLVMType, right.LLVMValue);
                    }
                    else
                    {
                        rightVal = right.LLVMValue;
                    }

                    switch (binaryOp.Type)
                    {
                        case OperationType.Addition:
                            builtVal = compiler.Builder.BuildAdd(leftVal, rightVal);
                            break;
                        case OperationType.Subtraction:
                            builtVal = compiler.Builder.BuildSub(leftVal, rightVal);
                            break;
                        case OperationType.Multiplication:
                            builtVal = compiler.Builder.BuildMul(leftVal, rightVal);
                            break;
                        case OperationType.Division:
                            builtVal = compiler.Builder.BuildSDiv(leftVal, rightVal);
                            break;
                        case OperationType.Equal:
                            if (left.LLVMType == LLVMTypeRef.Float)
                            {
                                builtVal = compiler.Builder.BuildFCmp(LLVMRealPredicate.LLVMRealOEQ, leftVal, rightVal);
                            }
                            else if (left.LLVMType == LLVMTypeRef.Int32)
                            {
                                builtVal = compiler.Builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, leftVal, rightVal);
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }

                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    return new ValueContext(left.LLVMType, builtVal, left.ClassOfType);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
        else if (expr is ConstantNode constNode)
        {
            if (constNode.Value is string str)
            {
                if (compiler.Classes.TryGetValue("str", out Class @class)) {
                    var constStr = compiler.Context.GetConstString(str, false);
                    var global = compiler.Module.AddGlobal(constStr.TypeOf, "");
                    global.Initializer = constStr;
                    return new ValueContext(@class.LLVMType, global, @class);
                }
                else
                {
                    throw new Exception("Critical failure: compiler cannot find the primitive type \"str\".");
                }
            }
            else if (constNode.Value is int i32)
            {
                if (compiler.Classes.TryGetValue("i32", out Class @class))
                {
                    return new ValueContext(@class.LLVMType, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)i32), @class);
                }
                else
                {
                    throw new Exception("Critical failure: compiler cannot find the primitive type \"i32\".");
                }
            }
            else if (constNode.Value is float f32)
            {
                if (compiler.Classes.TryGetValue("f32", out Class @class))
                {
                    return new ValueContext(@class.LLVMType, LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, f32), @class);
                }
                else
                {
                    throw new Exception("Critical failure: compiler cannot find the primitive type \"f32\".");
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else if (expr is RefNode @ref)
        {
            return CompileRef(compiler, scope, @ref);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static ValueContext CompileRef(CompilerContext compiler, Scope scope, RefNode refNode) //TODO: add support for statics
    {
        ValueContext context = null;

        while (refNode != null)
        {
            if (refNode is ThisNode)
            {
                if (compiler.CurrentFunction.OwnerClass == null)
                {
                    throw new Exception("Attempted self-instance reference in a global function.");
                }

                context = new ValueContext(compiler.CurrentFunction.OwnerClass.LLVMType,
                    compiler.CurrentFunction.LLVMFunc.FirstParam,
                    compiler.CurrentFunction.OwnerClass);
                refNode = refNode.Child;
            }
            else if (refNode is TypeRefNode typeRef)
            {
                if (compiler.Classes.TryGetValue(typeRef.Name, out Class @class))
                {
                    refNode = refNode.Child;

                    if (refNode is MethodCallNode methodCall)
                    {
                        context = CompileFuncCall(compiler, context, scope, methodCall, @class);
                        refNode = refNode.Child;
                    }
                    else
                    {
                        if (@class.StaticFields.TryGetValue(refNode.Name, out Field field))
                        {
                            throw new NotImplementedException();
                        }
                        else
                        {
                            throw new Exception($"Field {refNode.Name} does not exist as static on the class {@class.Name}.");
                        }
                    }
                }
                else
                {
                    throw new Exception($"Type \"{typeRef.Name}\" does not exist.");
                }
            }
            else if (refNode is MethodCallNode methodCall)
            {
                context = CompileFuncCall(compiler, context, scope, methodCall);
                refNode = refNode.Child;
            }
            else if (refNode is IndexAccessNode indexAccess)
            {
                throw new NotImplementedException("Index access is not currently available."); //TODO
            }
            else
            {
                if (context != null)
                {
                    if (context.ClassOfType.Fields.TryGetValue(refNode.Name, out Field field))
                    {
                        context = new ValueContext(field.LLVMType,
                            compiler.Builder.BuildStructGEP2(context.LLVMType,
                                context.LLVMValue,
                                field.FieldIndex),
                            field.ClassOfType);
                        refNode = refNode.Child;
                    }
                    else
                    {
                        throw new Exception($"Field \"{refNode.Name}\" does not exist.");
                    }
                }
                else
                {
                    if (scope.Variables.TryGetValue(refNode.Name, out Variable @var))
                    {
                        context = new ValueContext(@var.LLVMType,
                            compiler.Builder.BuildLoad2(@var.LLVMType, @var.LLVMVariable),
                            @var.ClassOfType);
                        refNode = refNode.Child;
                    }
                    else
                    {
                        if (compiler.CurrentFunction.OwnerClass != null)
                        {
                            context = new ValueContext(compiler.CurrentFunction.OwnerClass.LLVMType,
                                compiler.CurrentFunction.LLVMFunc.FirstParam,
                                compiler.CurrentFunction.OwnerClass);

                            if (context.ClassOfType.Fields.TryGetValue(refNode.Name, out Field field))
                            {
                                context = new ValueContext(field.LLVMType,
                                    compiler.Builder.BuildStructGEP2(context.LLVMType,
                                        context.LLVMValue,
                                        field.FieldIndex),
                                    field.ClassOfType);
                                refNode = refNode.Child;
                            }
                        }
                        else
                        {
                            throw new Exception($"Variable \"{refNode.Name}\" does not exist.");
                        }
                        
                    }
                }
            }
        }

        return context;
    }

    public static ValueContext CompileFuncCall(CompilerContext compiler, ValueContext context, Scope scope, MethodCallNode methodCall,
        Class staticClass = null)
    {
        List<LLVMValueRef> args = new List<LLVMValueRef>();
        Function func;

        if (context == null)
        {
            if (compiler.CurrentFunction.OwnerClass != null)
            {
                context = new ValueContext(compiler.CurrentFunction.OwnerClass.LLVMType,
                    compiler.CurrentFunction.LLVMFunc.FirstParam,
                    compiler.CurrentFunction.OwnerClass);
            }
        }

        if (context != null && context.ClassOfType.Methods.TryGetValue(methodCall.Name, out func))
        {
            if (context.LLVMType == func.LLVMFuncType.ParamTypes[0])
            {
                args.Add(context.LLVMValue);
            }
            else
            {
                throw new Exception("Attempted to call a method on a different class. !!THIS IS NOT A USER ERROR. REPORT ASAP!!");
            }
        }
        else if (context == null && compiler.GlobalFunctions.TryGetValue(methodCall.Name, out func))
        {
            // Keep empty
        }
        else if (staticClass != null && staticClass.StaticMethods.TryGetValue(methodCall.Name, out func))
        {
            // Keep empty
        }
        else
        {
            throw new Exception($"Function \"{methodCall.Name}\" does not exist.");
        }

        foreach (ExpressionNode arg in methodCall.Arguments)
        {
            var val = CompileExpression(compiler, scope, arg);
            args.Add(val.LLVMValue);
        }

        return new ValueContext(func.ClassOfReturnType.LLVMType,
            compiler.Builder.BuildCall2(func.LLVMFuncType, func.LLVMFunc, args.ToArray()),
            func.ClassOfReturnType);
    }
}