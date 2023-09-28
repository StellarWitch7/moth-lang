using LLVMSharp.Interop;
using Moth.AST;
using Moth.AST.Node;
using Moth.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
                    foreach (MethodDefNode methodDef in classNode.Scope.Statements.OfType<MethodDefNode>())
                    {
                        DefineMethod(compiler, @class, methodDef);
                    }
                }
                else
                {
                    throw new Exception("Tried to get a class that doesn't exist. This is a critical error that must be reported.");
                }
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
                    foreach (MethodDefNode methodDef in classNode.Scope.Statements.OfType<MethodDefNode>())
                    {
                        CompileMethod(compiler, methodDef);
                    }
                }
                else
                {
                    throw new Exception("Tried to get a class that doesn't exist. This is a critical error that must be reported.");
                }
            }
        }
    }

    public static void DefineClass(CompilerContext compiler, ClassNode classNode)
    {
        LLVMTypeRef newStruct = compiler.Context.CreateNamedStruct(classNode.Name);
        compiler.Classes.Add(classNode.Name, new Class(newStruct, classNode.Privacy));
    }

    public static void CompileClass(CompilerContext compiler, ClassNode classNode)
    {
        uint index = 0;
        List<LLVMTypeRef> lLVMTypes = new List<LLVMTypeRef>();

        if (compiler.Classes.TryGetValue(classNode.Name, out Class @class))
        {
            foreach (FieldNode field in classNode.Scope.Statements.OfType<FieldNode>())
            {
                if (compiler.Classes.TryGetValue(field.TypeRef, out Class type))
                {
                    lLVMTypes.Add(type.LLVMType);
                    @class.Fields.Add(field.Name, new Field(index, type.LLVMType, field.Privacy, type, field.IsConstant));
                    index++;
                }
            }

            @class.LLVMType.StructSetBody(lLVMTypes.ToArray(), false);
            compiler.CurrentClass = @class;
        }
        else
        {
            throw new Exception("Tried to get a class that doesn't exist. This is a critical error that must be reported.");
        }
    }

    public static void DefineMethod(CompilerContext compiler, Class @class, MethodDefNode methodDef)
    {
        int index = 1;
        List<Parameter> @params = new List<Parameter>();
        List<LLVMTypeRef> paramTypes = new List<LLVMTypeRef> { LLVMTypeRef.CreatePointer(@class.LLVMType, 0) };

        foreach (ParameterNode param in methodDef.Params)
        {
            if (compiler.Classes.TryGetValue(param.TypeRef, out Class type))
            @params.Add(new Parameter(index, param.Name, type.LLVMType, type));
            paramTypes.Add(type.LLVMType);
            index++;
        }

        if (compiler.Classes.TryGetValue(methodDef.ReturnTypeRef, out Class returnType))
        {

            LLVMTypeRef lLVMFuncType = LLVMTypeRef.CreateFunction(returnType.LLVMType, paramTypes.ToArray());
            LLVMValueRef lLVMFunc = compiler.Module.AddFunction(methodDef.Name, lLVMFuncType);
            Function func = new Function(lLVMFunc,
                lLVMFuncType,
                returnType.LLVMType,
                methodDef.Privacy,
                returnType,
                @params);
            @class.Functions.Add(methodDef.Name, func);
        }
    }

    public static void CompileMethod(CompilerContext compiler, MethodDefNode methodDef)
    {
        var @class = compiler.CurrentClass;
        
        if (@class.Functions.TryGetValue(methodDef.Name, out Function func))
        {
            func.OpeningScope = new Scope(func.LLVMFunc.AppendBasicBlock("entry"));
            compiler.Builder.PositionAtEnd(func.OpeningScope.LLVMBlock);
            compiler.CurrentFunction = func;

            foreach (Parameter param in compiler.CurrentFunction.Params)
            {
                var paramAsVar = compiler.Builder.BuildAlloca(param.LLVMType, param.Name);
                compiler.Builder.BuildStore(compiler.CurrentFunction.LLVMFunc.Params[param.ParamIndex], paramAsVar);
                func.OpeningScope.Variables.Add(param.Name,
                    new Variable(paramAsVar,
                        param.LLVMType,
                        PrivacyType.Local,
                        param.ClassOfType,
                        false));
            }

            if (!CompileScope(compiler, func.OpeningScope, methodDef.ExecutionBlock))
            {
                throw new Exception("Function is not guaranteed to return.");
            }
        }
        else
        {
            throw new Exception();
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
            else if (statement is FieldNode fieldDef)
            {
                if (fieldDef is InferredLocalDefNode inferredDef)
                {
                    var result = CompileExpression(compiler, scope, inferredDef.DefaultValue);
                    var lLVMVar = compiler.Builder.BuildAlloca(result.LLVMType, fieldDef.Name);
                    compiler.Builder.BuildStore(result.LLVMValue, lLVMVar);
                    scope.Variables.Add(fieldDef.Name,
                        new Variable(lLVMVar,
                            result.LLVMType,
                            fieldDef.Privacy,
                            result.ClassOfType,
                            fieldDef.IsConstant));
                }
                else if (compiler.Classes.TryGetValue(fieldDef.TypeRef, out Class type))
                {
                    var lLVMVar = compiler.Builder.BuildAlloca(type.LLVMType, fieldDef.Name);
                    scope.Variables.Add(fieldDef.Name,
                        new Variable(lLVMVar,
                            type.LLVMType,
                            fieldDef.Privacy,
                            type,
                            fieldDef.IsConstant));
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
                if (compiler.Classes.TryGetValue("string", out Class @class)) {
                    return new ValueContext(@class.LLVMType, compiler.Context.GetConstString(str, false), @class);
                }
                else
                {
                    throw new Exception("Critical failure: compiler cannot find the primitive type \"string\".");
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

    public static ValueContext CompileRef(CompilerContext compiler, Scope scope, RefNode refNode)
    {
        throw new NotImplementedException();

        //AccessContext context = null;

        //while (refNode != null)
        //{
        //    if (refNode is ThisNode)
        //    {
        //        if (compiler.CurrentFunction.IsGlobal)
        //        {
        //            throw new Exception("Attempted self-instance reference in a global function.");
        //        }

        //        context = new AccessContext(new ValueContext(compiler.CurrentClass.LLVMType,
        //                compiler.CurrentFunction.LLVMFunc.FirstParam),
        //            compiler.CurrentClass);
        //        refNode = refNode.Child;
        //    }
        //    else if (refNode is MethodCallNode methodCall)
        //    {
        //        if (context == null)
        //        {
        //            if (compiler.CurrentFunction.IsGlobal)
        //            {
        //                throw new Exception("Attempted self-instance reference in a global function.");
        //            }

        //            context = new AccessContext(new ValueContext(compiler.CurrentClass.LLVMType,
        //                    compiler.CurrentFunction.LLVMFunc.FirstParam),
        //                compiler.CurrentClass);
        //        }

        //        if (context.Class.Functions.TryGetValue(methodCall.Name, out Function func))
        //        {
        //            List<LLVMValueRef> args = new List<LLVMValueRef>();

        //            foreach (ExpressionNode arg in methodCall.Arguments)
        //            {
        //                var val = CompileExpression(compiler, scope, arg);
        //                args.Add(val.LLVMValue);
        //            }

        //            if (compiler.Classes.TryGetValue(func.ClassOfReturnType.Name, out Class returnClass))
        //            {
        //                context = new AccessContext(new DefTypedValueContext(compiler,
        //                        func.ClassOfReturnType,
        //                        compiler.Builder.BuildCall2(func.LLVMFuncType, func.LLVMFunc, args.ToArray())),
        //                    returnClass);
        //                refNode = refNode.Child;
        //            }
        //            else
        //            {
        //                throw new Exception("Called function with invalid return type.");
        //            }
        //        }
        //        else
        //        {
        //            throw new Exception("Function does not exist.");
        //        }
        //    }
        //    else if (refNode is IndexAccessNode indexAccess)
        //    {
        //        throw new NotImplementedException("Index access is not currently available."); //TODO
        //    }
        //    else
        //    {
        //        if (context != null)
        //        {
        //            if (context.Class.Fields.TryGetValue(refNode.Name, out Field field))
        //            {
        //                if (compiler.Classes.TryGetValue(field.TypeRef.Name, out Class fieldClass))
        //                {
        //                    context = new AccessContext(new DefTypedValueContext(compiler,
        //                            field.TypeRef,
        //                            compiler.Builder.BuildStructGEP2(context.ValueContext.LLVMType,
        //                                context.ValueContext.LLVMValue,
        //                                field.FieldIndex)),
        //                        fieldClass);
        //                    refNode = refNode.Child;
        //                }
        //                else
        //                {
        //                    throw new Exception("Attempted to load a field with an invalid type.");
        //                }
        //            }
        //            else
        //            {
        //                throw new Exception("Field does not exist.");
        //            }
        //        }
        //        else
        //        {
        //            if (scope.Variables.TryGetValue(refNode.Name, out Variable @var))
        //            {
        //                if (compiler.Classes.TryGetValue(@var.ClassOfType.Name, out Class varClass))
        //                {
        //                    context = new AccessContext(new DefTypedValueContext(compiler,
        //                            @var.ClassOfType,
        //                            compiler.Builder.BuildLoad2(@var.LLVMType, @var.LLVMVariable)),
        //                        varClass);
        //                    refNode = refNode.Child;
        //                }
        //                else
        //                {
        //                    throw new Exception("Attempted to load a variable with an invalid type.");
        //                }
        //            }
        //            else if (compiler.CurrentClass.Fields.TryGetValue(refNode.Name, out Field field))
        //            {
        //                if (compiler.Classes.TryGetValue(field.TypeRef.Name, out Class fieldClass))
        //                {
        //                    context = new AccessContext(new ValueContext(compiler.CurrentClass.LLVMType,
        //                            compiler.CurrentFunction.LLVMFunc.FirstParam),
        //                        compiler.CurrentClass);
        //                    context = new AccessContext(new DefTypedValueContext(compiler,
        //                            field.TypeRef,
        //                            compiler.Builder.BuildStructGEP2(context.ValueContext.LLVMType,
        //                                context.ValueContext.LLVMValue,
        //                                field.FieldIndex)),
        //                        fieldClass);
        //                    refNode = refNode.Child;
        //                }
        //                else
        //                {
        //                    throw new Exception("Attempted to load a field with an invalid type.");
        //                }
        //            }
        //            else
        //            {
        //                throw new Exception("Variable not found.");
        //            }
        //        }
        //    }
        //}

        //if (context.ValueContext is DefTypedValueContext defTypedContext)
        //{
        //    return defTypedContext;
        //}
        //else
        //{
        //    throw new NotImplementedException();
        //}
    }
}