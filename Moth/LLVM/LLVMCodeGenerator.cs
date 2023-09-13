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

public static class LLVMCodeGenerator
{
    public static void ConvertScript(CompilerContext compiler, ScriptAST script)
    {
        foreach (var @class in script.ClassNodes)
        {
            DefineClass(compiler, @class);
        }

        foreach (var @class in script.ClassNodes)
        {
            ConvertClass(compiler, @class);
        }
    }

    public static void DefineClass(CompilerContext compiler, ClassNode classNode)
    {
        LLVMTypeRef newStruct = compiler.Context.CreateNamedStruct(classNode.Name);
        compiler.Classes.Add(classNode.Name, new Class(newStruct, classNode.Privacy));
    }

    public static void ConvertClass(CompilerContext compiler, ClassNode classNode)
    {
        List<LLVMTypeRef> types = new List<LLVMTypeRef>();

        foreach (FieldNode field in classNode.Scope.Statements.OfType<FieldNode>())
        {
            types.Add(DefToLLVMType(compiler, field.Type, field.TypeRef));
        }

        compiler.Classes.TryGetValue(classNode.Name, out Class @class);
        @class.LLVMClass.StructSetBody(types.ToArray(), false);
        compiler.CurrentClass = @class;

        foreach (MethodDefNode methodDef in classNode.Scope.Statements.OfType<MethodDefNode>())
        {
            DefineMethod(compiler, @class, methodDef);
        }

        foreach (MethodDefNode methodDef in classNode.Scope.Statements.OfType<MethodDefNode>())
        {
            ConvertMethod(compiler, methodDef);
        }
    }

    public static void DefineMethod(CompilerContext compiler, Class @class, MethodDefNode methodDef)
    {
        List<LLVMTypeRef> paramTypes = new List<LLVMTypeRef> { LLVMTypeRef.CreatePointer(@class.LLVMClass, 0) };

        foreach (ParameterNode param in methodDef.Params)
        {
            paramTypes.Add(DefToLLVMType(compiler, param.Type, param.TypeRef));
        }

        var funcType = LLVMTypeRef.CreateFunction(DefToLLVMType(compiler, methodDef.ReturnType, methodDef.ReturnObject), paramTypes.ToArray());
        LLVMValueRef func = compiler.Module.AddFunction(methodDef.Name, funcType);
        @class.Functions.Add(methodDef.Name, new Function(func, methodDef.Privacy));
    }

    public static void ConvertMethod(CompilerContext compiler, MethodDefNode methodDef)
    {
        var @class = compiler.CurrentClass;
        @class.Functions.TryGetValue(methodDef.Name, out Function func);
        func.OpeningScope = new Scope(func.LLVMFunc.AppendBasicBlock(""));
        compiler.Builder.PositionAtEnd(func.OpeningScope.LLVMBlock);
        compiler.CurrentFunction = func;

        foreach (ParameterNode param in methodDef.Params)
        {
            var lLLVMType = DefToLLVMType(compiler, param.Type, param.TypeRef);
            func.OpeningScope.Variables.Add(param.Name,
                new Variable(compiler.Builder.BuildAlloca(lLLVMType,
                    param.Name),
                lLLVMType,
                PrivacyType.Local,
                param.Type,
                param.TypeRef,
                false));
        }

        ConvertScope(compiler, func.OpeningScope, methodDef.ExecutionBlock);
    }

    public static void ConvertScope(CompilerContext compiler, Scope scope, ScopeNode scopeNode)
    {
        compiler.Builder.PositionAtEnd(scope.LLVMBlock);

        foreach (StatementNode statement in scopeNode.Statements)
        {
            if (statement is FieldNode fieldDef)
            {
                var lLVMType = DefToLLVMType(compiler, fieldDef.Type, fieldDef.TypeRef);
                scope.Variables.Add(fieldDef.Name,
                    new Variable(compiler.Builder.BuildAlloca(lLVMType,
                        fieldDef.Name),
                    lLVMType,
                    fieldDef.Privacy,
                    fieldDef.Type,
                    fieldDef.TypeRef,
                    fieldDef.IsConstant));
            }
            else if (statement is ScopeNode newScopeNode)
            {
                var newScope = new Scope(scope.LLVMBlock.InsertBasicBlock(""));
                newScope.Variables = scope.Variables;
                ConvertScope(compiler, newScope, newScopeNode);
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
    }

    public static LLVMValueRef CompileExpression(CompilerContext compiler, Scope scope, ExpressionNode exprNode)
    {
        if (exprNode is BinaryOperationNode binaryOp)
        {
            if (binaryOp.Type == OperationType.Assignment)
            {
                if (binaryOp.Left is RefNode @ref)
                {
                    return compiler.Builder.BuildStore(CompileExpression(compiler, scope, binaryOp.Right), CompileRef(compiler, scope, @ref));
                }
                else
                {
                    throw new Exception();
                }
            }
            else if (binaryOp.Type == OperationType.Addition)
            {
                return compiler.Builder.BuildAdd(CompileExpression(compiler, scope, binaryOp.Left),
                    CompileExpression(compiler, scope, binaryOp.Right));
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
        else if (exprNode is ConstantNode constNode)
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
        else if (exprNode is RefNode @ref)
        {
            return CompileRef(compiler, scope, @ref);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static LLVMValueRef CompileRef(CompilerContext compiler, Scope scope, RefNode refNode)
    {
        object currentLocation;

        if (refNode is VariableRefNode varRef)
        {
            if (varRef.IsLocalVar)
            {
                if (scope.Variables.TryGetValue(varRef.Name, out Variable @var))
                {
                    currentLocation = @var; //do I need to create the BuildLoad2 yet? How it's done:
                                            //compiler.Builder.BuildLoad2(@var.LLVMType, @var.LLVMVariable)
                }
                else
                {
                    throw new Exception("Local variable does not exist in the current scope.");
                }
            }
            else
            {
                throw new Exception("How in all hell did this manage to happen???? Non-local variable has no origin prefix.");
            }
        }
        else if (refNode is ClassRefNode classRef)
        {
            if (classRef.IsCurrentClass)
            {
                currentLocation = compiler.CurrentClass;
            }
            else
            {
                if (compiler.Classes.TryGetValue(classRef.Name, out Class @class))
                {
                    currentLocation = @class;
                }
                else
                {
                    throw new Exception("Class does not exist.");
                }
            }
        }
        else if (refNode is MethodCallNode methodCall)
        {
            throw new NotImplementedException();
        }
        else
        {
            throw new Exception("Pretty sure that doesn't exist... how'd you manage that?"
                + "Access operation does not begin with a local variable, class, or method call.");
        }

        refNode = refNode.Child;

        while (refNode.Child != null)
        {
            if (refNode is ClassRefNode classRefNode)
            {

            }
        }
    }

    public static LLVMTypeRef DefToLLVMType(CompilerContext compiler, DefinitionType definitionType, ClassRefNode? classRef = null)
    {
        switch (definitionType)
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
                compiler.Classes.TryGetValue(classRef.Name, out Class? @class);
                if (@class == null) throw new Exception("Attempted to reference a class that does not exist in the current environment.");
                return @class.LLVMClass;
            default:
                throw new NotImplementedException(); //can't handle other types D:
        }
    }
}
