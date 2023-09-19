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
    public static void CompileScript(CompilerContext compiler, ScriptAST script)
    {
        foreach (var @class in script.ClassNodes)
        {
            DefineClass(compiler, @class);
        }

        foreach (var @class in script.ClassNodes)
        {
            CompileClass(compiler, @class);
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
        compiler.Classes.TryGetValue(classNode.Name, out Class @class);

        foreach (FieldNode field in classNode.Scope.Statements.OfType<FieldNode>())
        {
            var lLVMType = DefToLLVMType(compiler, field.TypeRef);
            lLVMTypes.Add(lLVMType);
            @class.Fields.Add(field.Name, new Field(index, lLVMType, field.Privacy, field.TypeRef, field.IsConstant));
            index++;
        }

        @class.LLVMClass.StructSetBody(lLVMTypes.ToArray(), false);
        compiler.CurrentClass = @class;

        foreach (MethodDefNode methodDef in classNode.Scope.Statements.OfType<MethodDefNode>())
        {
            DefineMethod(compiler, @class, methodDef);
        }

        foreach (MethodDefNode methodDef in classNode.Scope.Statements.OfType<MethodDefNode>())
        {
            CompileMethod(compiler, methodDef);
        }
    }

    public static void DefineMethod(CompilerContext compiler, Class @class, MethodDefNode methodDef)
    {
        int index = 1;
        List<Parameter> @params = new List<Parameter>();
        List<LLVMTypeRef> paramTypes = new List<LLVMTypeRef> { LLVMTypeRef.CreatePointer(@class.LLVMClass, 0) };

        foreach (ParameterNode param in methodDef.Params)
        {
            LLVMTypeRef lLVMType = DefToLLVMType(compiler, param.TypeRef);
            @params.Add(new Parameter(index, param.Name, lLVMType, param.TypeRef));
            paramTypes.Add(lLVMType);
            index++;
        }

        LLVMTypeRef lLVMReturnType = DefToLLVMType(compiler, methodDef.ReturnTypeRef);
        LLVMTypeRef lLVMFuncType = LLVMTypeRef.CreateFunction(lLVMReturnType, paramTypes.ToArray());
        LLVMValueRef lLVMFunc = compiler.Module.AddFunction(methodDef.Name, lLVMFuncType);
        Function func = new Function(lLVMFunc, lLVMFuncType, lLVMReturnType, methodDef.Privacy, methodDef.ReturnTypeRef, @params);
        func.Params = @params;
        @class.Functions.Add(methodDef.Name, func);
    }

    public static void CompileMethod(CompilerContext compiler, MethodDefNode methodDef)
    {
        var @class = compiler.CurrentClass;
        
        if (@class.Functions.TryGetValue(methodDef.Name, out Function func))
        {
            func.OpeningScope = new Scope(func.LLVMFunc.AppendBasicBlock(""));
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
                        param.TypeRef,
                        false));
            }

            CompileScope(compiler, func.OpeningScope, methodDef.ExecutionBlock);
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
                var lLVMType = DefToLLVMType(compiler, fieldDef.TypeRef);
                scope.Variables.Add(fieldDef.Name,
                    new Variable(compiler.Builder.BuildAlloca(lLVMType,
                        fieldDef.Name),
                        lLVMType,
                        fieldDef.Privacy,
                        fieldDef.TypeRef,
                        fieldDef.IsConstant));
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
            //else if (statement is IfNode @if)
            //{
            //    LLVMValueRef condition = ConvertExpression(compiler, @if.Condition); //I don't know what the hell I'm doing

            //    var then = compiler.Context.AppendBasicBlock(func.LLVMFunc, "then");
            //    var @else = compiler.Context.AppendBasicBlock(func.LLVMFunc, "else");
            //    var @continue = compiler.Context.AppendBasicBlock(func.LLVMFunc, "continue");

            //    compiler.Builder.BuildCondBr(condition, then, @else);

            //    //then
            //    {
            //        compiler.Builder.PositionAtEnd(then);
            //        ConvertScope(compiler, func, @if.Then);
            //        compiler.Builder.BuildBr(@continue);
            //    }

            //    //else
            //    {
            //        compiler.Builder.PositionAtEnd(@else);

            //        if (@if.Else != null)
            //        {
            //            ConvertScope(compiler, func, @if.Else);
            //        }

            //        compiler.Builder.BuildBr(@continue);
            //    }

            //    //continue
            //    {
            //        compiler.Builder.PositionAtEnd(@continue);
            //    }
            //}
            else if (statement is ExpressionNode exprNode)
            {
                CompileExpression(compiler, scope, exprNode);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        //if (compiler.CurrentFunction.ReturnType.Type == DefinitionType.Void)
        //{
        //    compiler.Builder.BuildRetVoid();
        //}
        //else
        //{
        //    throw new Exception("Function has no return.");
        //}

        return false;
    }

    public static DefTypedValueContext CompileExpression(CompilerContext compiler, Scope scope, ExpressionNode expr)
    {
        if (expr is BinaryOperationNode binaryOp)
        {
            if (binaryOp.Type == OperationType.Assignment)
            {
                if (binaryOp.Left is RefNode @ref)
                {
                    var variableAssigned = CompileRef(compiler, scope, @ref);
                    compiler.Builder.BuildStore(CompileExpression(compiler, scope, binaryOp.Right).LLVMValue, variableAssigned.LLVMValue);
                    return new DefTypedValueContext(compiler, variableAssigned.TypeRef, variableAssigned.LLVMValue);
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

                if (left.TypeRef.Type == right.TypeRef.Type && left.TypeRef.Type != DefinitionType.UnknownObject)
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
                        default:
                            throw new NotImplementedException();
                    }

                    return new DefTypedValueContext(compiler, left.TypeRef, builtVal);
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
                return new DefTypedValueContext(compiler, new TypeRefNode(DefinitionType.String),
                    compiler.Context.GetConstString(str, false));
            }
            else if (constNode.Value is int i32)
            {
                return new DefTypedValueContext(compiler, new TypeRefNode(DefinitionType.Int32),
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)i32));
            }
            else if (constNode.Value is float f32)
            {
                return new DefTypedValueContext(compiler, new TypeRefNode(DefinitionType.Float32),
                    LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, f32));
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

    public static DefTypedValueContext CompileRef(CompilerContext compiler, Scope scope, RefNode refNode)
    {
        AccessContext context = null;

        while (refNode != null)
        {
            if (refNode is ThisNode)
            {
                if (compiler.CurrentFunction.IsGlobal)
                {
                    throw new Exception("Attempted self-instance reference in a global function.");
                }

                context = new AccessContext(new ValueContext(compiler.CurrentClass.LLVMClass,
                        compiler.CurrentFunction.LLVMFunc.FirstParam),
                    compiler.CurrentClass);
                refNode = refNode.Child;
            }
            else if (refNode is MethodCallNode methodCall)
            {
                if (context == null)
                {
                    if (compiler.CurrentFunction.IsGlobal)
                    {
                        throw new Exception("Attempted self-instance reference in a global function.");
                    }

                    context = new AccessContext(new ValueContext(compiler.CurrentClass.LLVMClass,
                            compiler.CurrentFunction.LLVMFunc.FirstParam),
                        compiler.CurrentClass);
                }

                if (context.Class.Functions.TryGetValue(methodCall.Name, out Function func))
                {
                    List<LLVMValueRef> args = new List<LLVMValueRef>();

                    foreach (ExpressionNode arg in methodCall.Arguments)
                    {
                        var val = CompileExpression(compiler, scope, arg);
                        args.Add(val.LLVMValue);
                    }

                    if (compiler.Classes.TryGetValue(func.ReturnType.Name, out Class returnClass))
                    {
                        context = new AccessContext(new DefTypedValueContext(compiler,
                                func.ReturnType,
                                compiler.Builder.BuildCall2(func.LLVMFuncType, func.LLVMFunc, args.ToArray())),
                            returnClass);
                        refNode = refNode.Child;
                    }
                    else
                    {
                        throw new Exception("Called function with invalid return type.");
                    }
                }
                else
                {
                    throw new Exception("Function does not exist.");
                }
            }
            else if (refNode is IndexAccessNode indexAccess)
            {
                throw new NotImplementedException("Index access is not currently available."); //TODO
            }
            else
            {
                if (context != null)
                {
                    if (context.Class.Fields.TryGetValue(refNode.Name, out Field field))
                    {
                        if (compiler.Classes.TryGetValue(field.TypeRef.Name, out Class fieldClass))
                        {
                            context = new AccessContext(new DefTypedValueContext(compiler,
                                    field.TypeRef,
                                    compiler.Builder.BuildStructGEP2(context.ValueContext.LLVMType,
                                        context.ValueContext.Value,
                                        field.FieldIndex)),
                                fieldClass);
                            refNode = refNode.Child;
                        }
                        else
                        {
                            throw new Exception("Attempted to load a field with an invalid type.");
                        }
                    }
                    else
                    {
                        throw new Exception("Field does not exist.");
                    }
                }
                else
                {
                    if (scope.Variables.TryGetValue(refNode.Name, out Variable @var))
                    {
                        if (compiler.Classes.TryGetValue(@var.TypeRef.Name, out Class varClass))
                        {
                            context = new AccessContext(new DefTypedValueContext(compiler,
                                    @var.TypeRef,
                                    compiler.Builder.BuildLoad2(@var.LLVMType, @var.LLVMVariable)),
                                varClass);
                            refNode = refNode.Child;
                        }
                        else
                        {
                            throw new Exception("Attempted to load a variable with an invalid type.");
                        }
                    }
                    else if (compiler.CurrentClass.Fields.TryGetValue(refNode.Name, out Field field))
                    {
                        if (compiler.Classes.TryGetValue(field.TypeRef.Name, out Class fieldClass))
                        {
                            context = new AccessContext(new ValueContext(compiler.CurrentClass.LLVMClass,
                                    compiler.CurrentFunction.LLVMFunc.FirstParam),
                                compiler.CurrentClass);
                            context = new AccessContext(new DefTypedValueContext(compiler,
                                    field.TypeRef,
                                    compiler.Builder.BuildStructGEP2(context.ValueContext.LLVMType,
                                        context.ValueContext.Value,
                                        field.FieldIndex)),
                                fieldClass);
                            refNode = refNode.Child;
                        }
                        else
                        {
                            throw new Exception("Attempted to load a field with an invalid type.");
                        }
                    }
                    else
                    {
                        throw new Exception("Variable not found.");
                    }
                }
            }
        }

        if (context.ValueContext is DefTypedValueContext defTypedContext)
        {
            return defTypedContext;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static LLVMTypeRef DefToLLVMType(CompilerContext compiler, TypeRefNode typeRef)
    {
        switch (typeRef.Type)
        {
            case DefinitionType.Void:
                return LLVMTypeRef.Void;
            case DefinitionType.Int32:
                return LLVMTypeRef.Int32;
            case DefinitionType.Float32:
                return LLVMTypeRef.Float;
            case DefinitionType.Bool:
                return LLVMTypeRef.Int1;
            case DefinitionType.String:
                return LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0); //compiler.Context.GetConstString() for literal strings
            case DefinitionType.UnknownObject:
                compiler.Classes.TryGetValue(typeRef.Name, out Class? @class);
                if (@class == null) throw new Exception("Attempted to reference a class that does not exist in the current environment.");
                return @class.LLVMClass;
            default:
                throw new NotImplementedException(); //can't handle other types D:
        }
    }
}