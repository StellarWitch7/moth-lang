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

            foreach (var constDefNode in script.GlobalConstants)
            {
                DefineConstant(compiler, constDefNode);
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
        Class newClass = new Class(classNode.Name, newStruct, classNode.Privacy);
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
                    LLVMTypeRef fieldLLVMType = ResolveTypeRef(compiler, type, field.TypeRef);

                    lLVMTypes.Add(fieldLLVMType);
                    @class.Fields.Add(field.Name, new Field(field.Name, index, fieldLLVMType, type, field.Privacy));
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
        int index = 0;
        string funcName = funcDefNode.Name;
        List<Parameter> @params = new List<Parameter>();
        List<LLVMTypeRef> paramTypes = new List<LLVMTypeRef>();

        if (@class != null)
        {
            funcName = $"{@class.Name}.{funcName}";
        }

        if (@class != null && funcDefNode.Privacy != PrivacyType.Static)
        {
            paramTypes.Add(LLVMTypeRef.CreatePointer(@class.LLVMType, 0));
            index++;
        }

        foreach (ParameterNode paramNode in funcDefNode.Params)
        {
            if (compiler.Classes.TryGetValue(paramNode.TypeRef.Name, out Class type))
            {
                LLVMTypeRef paramLLVMType = ResolveTypeRef(compiler, type, paramNode.TypeRef);

                paramTypes.Add(paramLLVMType);
                @params.Add(new Parameter(index, paramNode.Name, paramLLVMType, type));
                index++;
            }
            else
            {
                throw new Exception($"Type {paramNode.TypeRef.Name} does not exist.");
            }
        }

        if (compiler.Classes.TryGetValue(funcDefNode.ReturnTypeRef.Name, out Class classOfReturnType))
        {
            LLVMTypeRef returnLLVMType = ResolveTypeRef(compiler, classOfReturnType, funcDefNode.ReturnTypeRef);
            LLVMTypeRef lLVMFuncType = LLVMTypeRef.CreateFunction(returnLLVMType, paramTypes.ToArray(), funcDefNode.IsVariadic);
            LLVMValueRef lLVMFunc = compiler.Module.AddFunction(funcName, lLVMFuncType);
            Function func = new Function(funcDefNode.Name,
                lLVMFunc,
                lLVMFuncType,
                returnLLVMType,
                classOfReturnType,
                funcDefNode.Privacy,
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

            foreach (var attribute in funcDefNode.Attributes)
            {
                ResolveAttribute(compiler, func, attribute);
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

        if (funcDefNode.Name == Reserved.Init && funcDefNode.Privacy == PrivacyType.Static)
        {
            if (func.ClassOfReturnType != func.OwnerClass)
            {
                throw new Exception($"Init method does not return the same type as its owner class (\"{func.OwnerClass.Name}\").");
            }

            var @new = new Variable(Reserved.Self,
                compiler.Builder.BuildAlloca(func.OwnerClass.LLVMType, Reserved.Self),
                func.OwnerClass.LLVMType,
                func.OwnerClass,
                PrivacyType.Local);

            func.OpeningScope.Variables.Add(@new.Name, @new);

            foreach (Field field in @new.ClassOfType.Fields.Values)
            {
                var lLVMField = compiler.Builder.BuildStructGEP2(@new.LLVMType, @new.LLVMVariable, field.FieldIndex);
                var zeroedVal = lLVMField.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind
                    ? LLVMValueRef.CreateConstPointerNull(field.LLVMType)
                    : LLVMValueRef.CreateConstNull(field.LLVMType);

                compiler.Builder.BuildStore(zeroedVal, lLVMField);
            }
        }

        foreach (Parameter param in compiler.CurrentFunction.Params)
        {
            var paramAsVar = compiler.Builder.BuildAlloca(param.LLVMType, param.Name);
            compiler.Builder.BuildStore(func.LLVMFunc.Params[param.ParamIndex], paramAsVar);
            func.OpeningScope.Variables.Add(param.Name,
                new Variable(param.Name,
                    paramAsVar,
                    param.LLVMType,
                    param.ClassOfType,
                    PrivacyType.Local));
        }

        if (!CompileScope(compiler, func.OpeningScope, funcDefNode.ExecutionBlock))
        {
            throw new Exception("Function is not guaranteed to return.");
        }
    }

    public static void DefineConstant(CompilerContext compiler, FieldDefNode constDef, Class @class = null)
    {
        if (compiler.Classes.TryGetValue(constDef.TypeRef.Name, out Class type))
        {
            var constLLVMType = ResolveTypeRef(compiler, type, constDef.TypeRef);
            var constVal = compiler.Module.AddGlobal(constLLVMType, constDef.Name);

            if (@class != null)
            {
                @class.Constants.Add(constDef.Name, new Constant(constLLVMType, constVal, type));
            }
            else
            {
                compiler.GlobalConstants.Add(constDef.Name, new Constant(constLLVMType, constVal, type));
            }
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
                    var expr = CompileExpression(compiler, scope, @return.ReturnValue);
                    compiler.Builder.BuildRet(SafeLoad(compiler, expr));
                }
                else
                {
                    compiler.Builder.BuildRetVoid();
                }

                return true;
            }
            else if (statement is LocalDefNode varDef)
            {
                if (varDef is InferredLocalDefNode inferredVarDef)
                {
                    var defaultVal = CompileExpression(compiler, scope, inferredVarDef.DefaultValue);
                    var lLVMVar = compiler.Builder.BuildAlloca(defaultVal.LLVMType, varDef.Name);
                    compiler.Builder.BuildStore(SafeLoad(compiler, defaultVal), lLVMVar);
                    scope.Variables.Add(varDef.Name,
                        new Variable(varDef.Name,
                            lLVMVar,
                            defaultVal.LLVMType,
                            defaultVal.ClassOfType,
                            varDef.Privacy));
                }
                else if (compiler.Classes.TryGetValue(varDef.TypeRef.Name, out Class type))
                {
                    LLVMTypeRef varLLVMType = ResolveTypeRef(compiler, type, varDef.TypeRef);

                    var lLVMVar = compiler.Builder.BuildAlloca(varLLVMType, varDef.Name);
                    scope.Variables.Add(varDef.Name, new Variable(varDef.Name, lLVMVar, varLLVMType, type, varDef.Privacy));

                    if (varDef.DefaultValue != null)
                    {
                        var value = CompileExpression(compiler, scope, varDef.DefaultValue);
                        compiler.Builder.BuildStore(SafeLoad(compiler, value), lLVMVar);
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
                newScope.Variables = new Dictionary<string, Variable>(scope.Variables);
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
            else if (statement is WhileNode @while)
            {
                var loop = compiler.CurrentFunction.LLVMFunc.AppendBasicBlock("loop");
                var then = compiler.CurrentFunction.LLVMFunc.AppendBasicBlock("then");
                var @continue = compiler.CurrentFunction.LLVMFunc.AppendBasicBlock("continue");
                compiler.Builder.BuildBr(loop);
                compiler.Builder.PositionAtEnd(loop);

                var condition = CompileExpression(compiler, scope, @while.Condition);
                compiler.Builder.BuildCondBr(SafeLoad(compiler, condition), then, @continue);
                compiler.Builder.PositionAtEnd(then);

                var newScope = new Scope(then);
                newScope.Variables = new Dictionary<string, Variable>(scope.Variables);
                
                if (!CompileScope(compiler, newScope, @while.Then))
                {
                    compiler.Builder.BuildBr(@loop);
                }
                
                compiler.Builder.PositionAtEnd(@continue);
                scope.LLVMBlock = @continue;
            }
            else if (statement is IfNode @if)
            {
                var condition = CompileExpression(compiler, scope, @if.Condition);
                var then = compiler.CurrentFunction.LLVMFunc.AppendBasicBlock("then");
                var @else = compiler.CurrentFunction.LLVMFunc.AppendBasicBlock("else");
                LLVMBasicBlockRef @continue = null;
                bool thenReturned = false;
                bool elseReturned = false;

                compiler.Builder.BuildCondBr(SafeLoad(compiler, condition), then, @else);

                //then
                {
                    compiler.Builder.PositionAtEnd(then);

                    {
                        var newScope = new Scope(then);
                        newScope.Variables = new Dictionary<string, Variable>(scope.Variables);
                        
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

                    var newScope = new Scope(@else);
                    newScope.Variables = new Dictionary<string, Variable>(scope.Variables);

                    if (@if.Else != null && CompileScope(compiler, newScope, @if.Else))
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

    private static LLVMTypeRef ResolveTypeRef(CompilerContext compiler, Class @class, TypeRefNode typeRef)
    {
        if (typeRef.IsPointer)
        {
            return LLVMTypeRef.CreatePointer(@class.LLVMType, 0);
        }
        else
        {
            return @class.LLVMType;
        }
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
                    var value = CompileExpression(compiler, scope, binaryOp.Right);
                    compiler.Builder.BuildStore(SafeLoad(compiler, value), variableAssigned.LLVMValue);
                    return new ValueContext(variableAssigned.LLVMType, variableAssigned.LLVMValue, variableAssigned.ClassOfType, true);
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

                if (/*left.ClassOfType == right.ClassOfType*/true) //TODO: whaaaa
                {
                    LLVMValueRef leftVal;
                    LLVMValueRef rightVal;
                    LLVMValueRef builtVal;

                    leftVal = SafeLoad(compiler, left);
                    rightVal = compiler.Builder.BuildCast(LLVMOpcode.LLVMBitCast, SafeLoad(compiler, right), left.LLVMType);

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
                            if (left.ClassOfType.Name == Reserved.Bool
                                || left.ClassOfType.Name == Reserved.Char
                                || left.ClassOfType.Name == Reserved.UnsignedInt16
                                || left.ClassOfType.Name == Reserved.UnsignedInt32
                                || left.ClassOfType.Name == Reserved.UnsignedInt64)
                            {
                                builtVal = compiler.Builder.BuildUDiv(leftVal, rightVal);
                            }
                            else
                            {
                                builtVal = compiler.Builder.BuildSDiv(leftVal, rightVal);
                            }
                            break;
                        case OperationType.And:
                            builtVal = compiler.Builder.BuildAnd(leftVal, rightVal);
                            break;
                        case OperationType.Or:
                            builtVal = compiler.Builder.BuildOr(leftVal, rightVal);
                            break;
                        case OperationType.Equal:
                        case OperationType.NotEqual:
                            if (right.LLVMValue.IsNull)
                            {
                                builtVal = binaryOp.Type == OperationType.Equal
                                    ? compiler.Builder.BuildIsNull(leftVal)
                                    : compiler.Builder.BuildIsNotNull(leftVal);
                            }
                            else if (left.LLVMValue.IsNull)
                            {
                                builtVal = binaryOp.Type == OperationType.Equal
                                    ? compiler.Builder.BuildIsNull(rightVal)
                                    : compiler.Builder.BuildIsNotNull(rightVal);
                            }
                            else if (left.ClassOfType.Name == Reserved.Float16
                                || left.ClassOfType.Name == Reserved.Float32
                                || left.ClassOfType.Name == Reserved.Float64)
                            {
                                builtVal = compiler.Builder.BuildFCmp(binaryOp.Type == OperationType.Equal
                                    ? LLVMRealPredicate.LLVMRealOEQ
                                    : LLVMRealPredicate.LLVMRealUNE,
                                    leftVal, rightVal);
                            }
                            else if (left.ClassOfType.Name == Reserved.Bool
                                || left.ClassOfType.Name == Reserved.Char
                                || left.ClassOfType.Name == Reserved.UnsignedInt16
                                || left.ClassOfType.Name == Reserved.UnsignedInt32
                                || left.ClassOfType.Name == Reserved.UnsignedInt64
                                || left.ClassOfType.Name == Reserved.SignedInt16
                                || left.ClassOfType.Name == Reserved.SignedInt32
                                || left.ClassOfType.Name == Reserved.SignedInt64)
                            {
                                builtVal = compiler.Builder.BuildICmp(binaryOp.Type == OperationType.Equal
                                    ? LLVMIntPredicate.LLVMIntEQ
                                    : LLVMIntPredicate.LLVMIntNE,
                                    leftVal, rightVal);
                            }
                            else
                            {
                                throw new NotImplementedException(); //TODO: can't check string equality
                            }

                            break;
                        case OperationType.LargerThan:
                        case OperationType.LargerThanOrEqual:
                        case OperationType.LessThan:
                        case OperationType.LessThanOrEqual:
                            if (left.ClassOfType.Name == Reserved.Float16
                                || left.ClassOfType.Name == Reserved.Float32
                                || left.ClassOfType.Name == Reserved.Float64)
                            {
                                builtVal = compiler.Builder.BuildFCmp(binaryOp.Type switch
                                {
                                    OperationType.LargerThan => LLVMRealPredicate.LLVMRealOGT,
                                    OperationType.LargerThanOrEqual => LLVMRealPredicate.LLVMRealOGE,
                                    OperationType.LessThan => LLVMRealPredicate.LLVMRealOLT,
                                    OperationType.LessThanOrEqual => LLVMRealPredicate.LLVMRealOLE,
                                    _ => throw new NotImplementedException(),
                                }, leftVal, rightVal);
                            }
                            else if (left.ClassOfType.Name == Reserved.Bool
                                || left.ClassOfType.Name == Reserved.Char
                                || left.ClassOfType.Name == Reserved.UnsignedInt16
                                || left.ClassOfType.Name == Reserved.UnsignedInt32
                                || left.ClassOfType.Name == Reserved.UnsignedInt64)
                            {
                                builtVal = compiler.Builder.BuildICmp(binaryOp.Type switch
                                {
                                    OperationType.LargerThan => LLVMIntPredicate.LLVMIntUGT,
                                    OperationType.LargerThanOrEqual => LLVMIntPredicate.LLVMIntUGE,
                                    OperationType.LessThan => LLVMIntPredicate.LLVMIntULT,
                                    OperationType.LessThanOrEqual => LLVMIntPredicate.LLVMIntULE,
                                    _ => throw new NotImplementedException(),
                                }, leftVal, rightVal);
                            }
                            else if (left.ClassOfType.Name == Reserved.SignedInt16
                                || left.ClassOfType.Name == Reserved.SignedInt32
                                || left.ClassOfType.Name == Reserved.SignedInt64)
                            {
                                builtVal = compiler.Builder.BuildICmp(binaryOp.Type switch
                                {
                                    OperationType.LargerThan => LLVMIntPredicate.LLVMIntSGT,
                                    OperationType.LargerThanOrEqual => LLVMIntPredicate.LLVMIntSGE,
                                    OperationType.LessThan => LLVMIntPredicate.LLVMIntSLT,
                                    OperationType.LessThanOrEqual => LLVMIntPredicate.LLVMIntSLE,
                                    _ => throw new NotImplementedException(),
                                }, leftVal, rightVal);
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }

                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    return new ValueContext(left.LLVMType, builtVal, left.ClassOfType, false);
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
                if (compiler.Classes.TryGetValue(Reserved.String, out Class @class)) {
                    var constStr = compiler.Context.GetConstString(str, false);
                    var global = compiler.Module.AddGlobal(constStr.TypeOf, "");
                    global.Initializer = constStr;
                    return new ValueContext(@class.LLVMType, global, @class, false);
                }
                else
                {
                    throw new Exception($"Critical failure: compiler cannot find the primitive type \"{Reserved.String}\".");
                }
            }
            else if (constNode.Value is bool @bool)
            {
                if (compiler.Classes.TryGetValue(Reserved.Bool, out Class @class))
                {
                    ulong i;

                    if (@bool)
                    {
                        i = 1;
                    }
                    else
                    {
                        i = 0;
                    }

                    return new ValueContext(@class.LLVMType, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, i), @class, false);
                }
                else
                {
                    throw new Exception($"Critical failure: compiler cannot find the primitive type \"{Reserved.Bool}\".");
                }
            }
            else if (constNode.Value is int i32)
            {
                if (compiler.Classes.TryGetValue(Reserved.SignedInt32, out Class @class))
                {
                    return new ValueContext(@class.LLVMType, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)i32), @class, false);
                }
                else
                {
                    throw new Exception($"Critical failure: compiler cannot find the primitive type \"{Reserved.SignedInt32}\".");
                }
            }
            else if (constNode.Value is float f32)
            {
                if (compiler.Classes.TryGetValue(Reserved.Float32, out Class @class))
                {
                    return new ValueContext(@class.LLVMType, LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, f32), @class, false);
                }
                else
                {
                    throw new Exception($"Critical failure: compiler cannot find the primitive type \"{Reserved.Float32}\".");
                }
            }
            else if (constNode.Value is char ch)
            {
                if (compiler.Classes.TryGetValue(Reserved.Char, out Class @class))
                {
                    return new ValueContext(@class.LLVMType, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, (ulong)ch), @class, false);
                }
                else
                {
                    throw new Exception($"Critical failure: compiler cannot find the primitive type \"{Reserved.Char}\".");
                }
            }
            else if (constNode.Value == null)
            {
                if (compiler.Classes.TryGetValue(Reserved.Char, out Class @class))
                {
                    return new ValueContext(@class.LLVMType, LLVMValueRef.CreateConstNull(@class.LLVMType), @class, false);
                }
                else
                {
                    throw new Exception($"Critical failure: compiler cannot find the primitive type \"{Reserved.Char}\".");
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else if (expr is InverseNode inverse)
        {
            var @ref = CompileRef(compiler, scope, inverse.Value);
            return new ValueContext(@ref.LLVMType,
                compiler.Builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ,
                    SafeLoad(compiler, @ref),
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0)),
                @ref.ClassOfType,
                false); //TODO: check validity of bool
        }
        else if (expr is IncrementVarNode incrementVar)
        {
            var @ref = CompileRef(compiler, scope, incrementVar.RefNode);
            var valToAdd = LLVMValueRef.CreateConstInt(@ref.LLVMType, 1); //TODO: float compat?
            compiler.Builder.BuildStore(compiler.Builder.BuildAdd(SafeLoad(compiler, @ref), valToAdd), @ref.LLVMValue);
            return new ValueContext(@ref.LLVMType,
                @ref.LLVMValue,
                @ref.ClassOfType,
                true);
        }
        else if (expr is DecrementVarNode decrementVar)
        {
            var @ref = CompileRef(compiler, scope, decrementVar.RefNode);
            var valToAdd = LLVMValueRef.CreateConstInt(@ref.LLVMType, 1); //TODO: float compat?
            compiler.Builder.BuildStore(compiler.Builder.BuildSub(SafeLoad(compiler, @ref), valToAdd), @ref.LLVMValue);
            return new ValueContext(@ref.LLVMType,
                @ref.LLVMValue,
                @ref.ClassOfType,
                true);
        }
        else if (expr is RefNode @ref)
        {
            return CompileRef(compiler, scope, @ref);
        }
        else if (expr is SubExprNode subExpr)
        {
            return CompileExpression(compiler, scope, subExpr.Expression);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static ValueContext CompileRef(CompilerContext compiler, Scope scope, RefNode refNode)
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

                if (compiler.CurrentFunction.Name == Reserved.Init)
                {
                    if (scope.Variables.TryGetValue(Reserved.Self, out Variable self))
                    {
                        context = new ValueContext(self.LLVMType,
                            self.LLVMVariable,
                            self.ClassOfType,
                            true);
                    }
                    else
                    {
                        throw new Exception("Critical failure: \"self\" does not exist within constructor. " +
                            "!!THIS IS NOT A USER ERROR. REPORT ASAP!!");
                    }
                }
                else
                {
                    context = new ValueContext(compiler.CurrentFunction.OwnerClass.LLVMType,
                        compiler.CurrentFunction.LLVMFunc.FirstParam,
                        compiler.CurrentFunction.OwnerClass,
                        true);
                }

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
                context = CompileVarRef(compiler, context, scope, refNode);
                context = new ValueContext(context.ClassOfType.LLVMType,
                    compiler.Builder.BuildInBoundsGEP2(context.ClassOfType.TrueLLVMType, context.LLVMValue, new LLVMValueRef[1]
                    {
                        compiler.Builder.BuildIntCast(SafeLoad(compiler,
                            CompileExpression(compiler, scope, indexAccess.Index)),
                            LLVMTypeRef.Int64)
                    }),
                    context.ClassOfType,
                    false); //TODO: check this bool's validity
                refNode = refNode.Child;
            }
            else
            {
                context = CompileVarRef(compiler, context, scope, refNode);
                refNode = refNode.Child;
            }
        }

        return context;
    }

    private static ValueContext CompileVarRef(CompilerContext compiler, ValueContext context, Scope scope, RefNode refNode)
    {
        if (context != null)
        {
            if (context.ClassOfType.Fields.TryGetValue(refNode.Name, out Field field))
            {
                return new ValueContext(field.LLVMType,
                    compiler.Builder.BuildStructGEP2(context.LLVMType,
                        context.LLVMValue,
                        field.FieldIndex),
                    field.ClassOfType,
                    true);
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
                return new ValueContext(@var.LLVMType,
                    @var.LLVMVariable,
                    @var.ClassOfType,
                    true);
            }
            else if (compiler.CurrentFunction.OwnerClass != null
                && compiler.CurrentFunction.OwnerClass.Fields.TryGetValue(refNode.Name, out Field field))
            {
                return new ValueContext(field.LLVMType,
                    compiler.Builder.BuildStructGEP2(context.LLVMType,
                        context.LLVMValue,
                        field.FieldIndex),
                    field.ClassOfType,
                    true);
            }
            else if (compiler.GlobalConstants.TryGetValue(refNode.Name, out Constant @const))
            {
                return new ValueContext(@const.LLVMType,
                    @const.LLVMValue,
                    @const.ClassOfType,
                    true);
            }
            else
            {
                throw new Exception($"Variable \"{refNode.Name}\" does not exist.");
            }
        }
    }

    public static ValueContext CompileFuncCall(CompilerContext compiler, ValueContext context, Scope scope, MethodCallNode methodCall,
        Class staticClass = null)
    {
        List<LLVMValueRef> args = new List<LLVMValueRef>();
        bool contextWasNull = context == null;
        Function func;

        if (context == null)
        {
            if (compiler.CurrentFunction.OwnerClass != null)
            {
                context = new ValueContext(compiler.CurrentFunction.OwnerClass.LLVMType,
                    compiler.CurrentFunction.LLVMFunc.FirstParam,
                    compiler.CurrentFunction.OwnerClass,
                    false);
            }
        }

        if (context != null && context.ClassOfType.Methods.TryGetValue(methodCall.Name, out func))
        {
            if (context.LLVMType == func.LLVMFuncType.ParamTypes[0])
            {
                args.Add(SafeLoad(compiler, context));
            }
            else
            {
                throw new Exception("Attempted to call a method on a different class. !!THIS IS NOT A USER ERROR. REPORT ASAP!!");
            }
        }
        else if (contextWasNull && compiler.GlobalFunctions.TryGetValue(methodCall.Name, out func))
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
            args.Add(SafeLoad(compiler, val));
        }

        return new ValueContext(func.ClassOfReturnType.LLVMType,
            compiler.Builder.BuildCall2(func.LLVMFuncType, func.LLVMFunc, args.ToArray()),
            func.ClassOfReturnType,
            true);
    }

    public static void ResolveAttribute(CompilerContext compiler, Function func, AttributeNode attribute)
    {
        if (attribute.Name == "CallingConvention")
        {
            if (attribute.Arguments.Count != 1)
            {
                throw new Exception("Attribute \"CallingConvention\" has too many arguments.");
            }

            if (attribute.Arguments[0] is ConstantNode constantNode)
            {
                if (constantNode.Value is string str)
                {
                    switch (str)
                    {
                        case "cdecl":
                            var lLVMFunc = func.LLVMFunc;
                            lLVMFunc.FunctionCallConv = 0;
                            break;
                    }
                }
                else
                {
                    throw new Exception("Attribute \"CallingConvention\" was passed a non-string.");
                }
            }
            else
            {
                throw new Exception("Attribute \"CallingConvention\" was passed a complex expression.");
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static LLVMValueRef SafeLoad(CompilerContext compiler, ValueContext value)
    {
        if (value.LLVMValue.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind
            && value.ClassOfType.Name != Reserved.Void
            && value.IsVariable)
        {
            return compiler.Builder.BuildLoad2(value.LLVMType, value.LLVMValue);
        }
        else
        {
            return value.LLVMValue;
        }
    }
}