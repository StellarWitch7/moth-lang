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
                    compiler.Builder.BuildRet(CompileExpression(compiler, scope, @return.ReturnValue));
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
                var newScope = new Scope(scope.LLVMBlock.InsertBasicBlock(""));
                newScope.Variables = scope.Variables; //TODO: fix it? maybe?
                compiler.Builder.BuildBr(newScope.LLVMBlock);
                compiler.Builder.PositionAtEnd(newScope.LLVMBlock);
                
                if (CompileScope(compiler, newScope, newScopeNode))
                {
                    return true;
                }

                scope.LLVMBlock = newScope.LLVMBlock.InsertBasicBlock("");
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

    public static LLVMValueRef CompileExpression(CompilerContext compiler, Scope scope, ExpressionNode expr)
    {
        if (expr is BinaryOperationNode binaryOp)
        {
            if (binaryOp.Type == OperationType.Assignment)
            {
                if (binaryOp.Left is RefNode @ref)
                {
                    var variableAssigned = CompileRef(compiler, scope, @ref).Pointer;
                    compiler.Builder.BuildStore(CompileExpression(compiler, scope, binaryOp.Right), variableAssigned);
                    return variableAssigned;
                }
                else
                {
                    throw new Exception();
                }
            }
            else if (binaryOp.Type == OperationType.Addition)
            {
                return compiler.Builder.BuildAdd(CompileExpression(compiler, scope, binaryOp.Left),
                    CompileExpression(compiler, scope, binaryOp.Right)); //TODO: add a check for pointers to load them
            }
            else if (binaryOp.Type == OperationType.Subtraction)
            {
                return compiler.Builder.BuildSub(CompileExpression(compiler, scope, binaryOp.Left),
                    CompileExpression(compiler, scope, binaryOp.Right));
            }
            else if (binaryOp.Type == OperationType.Multiplication)
            {
                return compiler.Builder.BuildMul(CompileExpression(compiler, scope, binaryOp.Left),
                    CompileExpression(compiler, scope, binaryOp.Right));
            }
            else if (binaryOp.Type == OperationType.Division) //only for signed ints
            {
                return compiler.Builder.BuildSDiv(CompileExpression(compiler, scope, binaryOp.Left),
                    CompileExpression(compiler, scope, binaryOp.Right));
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else if (expr is ConstantNode constNode)
        {
            if (constNode.Value is string str)
            {
                return compiler.Context.GetConstString(str, false);
            }
            else if (constNode.Value is int i32)
            {
                return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)i32);
            }
            else if (constNode.Value is float f32)
            {
                return LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, f32);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else if (expr is RefNode @ref)
        {
            return CompileRef(compiler, scope, @ref).Pointer;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static PointerContext CompileRef(CompilerContext compiler, Scope scope, RefNode refNode)
    {
        object currentLocation;
        PointerContext pointerContext;
        {
            if (refNode is ThisNode)
            {
                currentLocation = compiler.CurrentClass;
                pointerContext = new PointerContext(compiler.CurrentClass.LLVMClass, compiler.CurrentFunction.LLVMFunc.FirstParam);
            }
            else if (refNode is MethodCallNode methodCall)
            {
                throw new NotImplementedException();
            }
            else if (refNode is IndexAccessNode indexAccess)
            {
                throw new Exception("Index access before array retrieval.");
            }
            else
            {
                if (scope.Variables.TryGetValue(refNode.Name, out Variable @var))
                {
                    currentLocation = @var;
                    pointerContext = new PointerContext(@var.LLVMType, @var.LLVMVariable);
                }
                else
                {
                    throw new Exception("Local variable does not exist.");
                }
            }
        }

        if (refNode.Child != null)
        {
            refNode = refNode.Child;

            while (refNode.Child != null)
            {
                if (refNode is MethodCallNode methodCall)
                {
                    if (currentLocation is Variable @var)
                    {
                        pointerContext = new PointerContext(@var.LLVMType, compiler.Builder.BuildLoad2(@var.LLVMType, @var.LLVMVariable));

                        if (compiler.Classes.TryGetValue(@var.TypeRef.Name, out Class @class))
                        {
                            if (@class.Functions.TryGetValue(methodCall.Name, out Function func))
                            {
                                currentLocation = func;
                                refNode = refNode.Child;
                            }
                            else
                            {
                                throw new Exception();
                            }
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    else if (currentLocation is Field field)
                    {
                        pointerContext = new PointerContext(field.LLVMType,
                                compiler.Builder.BuildStructGEP2(pointerContext.Type, pointerContext.Pointer, field.FieldIndex));

                        if (compiler.Classes.TryGetValue(field.TypeRef.Name, out Class @class))
                        {
                            if (@class.Functions.TryGetValue(methodCall.Name, out Function func))
                            {
                                currentLocation = func;
                                refNode = refNode.Child;
                            }
                            else
                            {
                                throw new Exception();
                            }
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    else if (currentLocation is Function func)
                    {
                        List<LLVMValueRef> args = new List<LLVMValueRef>();

                        foreach (ExpressionNode expr in methodCall.Arguments)
                        {
                            args.Add(CompileExpression(compiler, scope, expr));
                        }

                        pointerContext = new PointerContext(func.LLVMReturnType,
                            compiler.Builder.BuildCall2(func.LLVMFuncType, func.LLVMFunc, args.ToArray()));

                        if (compiler.Classes.TryGetValue(func.ReturnType.Name, out Class @class))
                        {
                            if (@class.Functions.TryGetValue(methodCall.Name, out Function newFunc))
                            {
                                currentLocation = newFunc;
                                refNode = refNode.Child;
                            }
                            else
                            {
                                throw new Exception();
                            }
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    if (currentLocation is Variable @var)
                    {
                        pointerContext = new PointerContext(@var.LLVMType, compiler.Builder.BuildLoad2(@var.LLVMType, @var.LLVMVariable));

                        if (compiler.Classes.TryGetValue(@var.TypeRef.Name, out Class @class))
                        {
                            if (@class.Fields.TryGetValue(refNode.Name, out Field field))
                            {
                                currentLocation = @field;
                                refNode = refNode.Child;
                            }
                            else
                            {
                                throw new Exception();
                            }
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    else if (currentLocation is Field field)
                    {
                        if (pointerContext != null)
                        {
                            pointerContext = new PointerContext(field.LLVMType,
                                compiler.Builder.BuildStructGEP2(pointerContext.Type, pointerContext.Pointer, field.FieldIndex));

                            if (compiler.Classes.TryGetValue(field.TypeRef.Name, out Class @class))
                            {
                                if (@class.Fields.TryGetValue(refNode.Name, out Field newField))
                                {
                                    currentLocation = newField;
                                    refNode = refNode.Child;
                                }
                                else
                                {
                                    throw new Exception();
                                }
                            }
                            else
                            {
                                throw new Exception();
                            }
                        }
                        else
                        {
                            throw new Exception("That is just idiotic. You tried accessing a field on... nothing?");
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        return pointerContext;
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
            case DefinitionType.ClassObject:
                compiler.Classes.TryGetValue(typeRef.Name, out Class? @class);
                if (@class == null) throw new Exception("Attempted to reference a class that does not exist in the current environment.");
                return @class.LLVMClass;
            default:
                throw new NotImplementedException(); //can't handle other types D:
        }
    }
}