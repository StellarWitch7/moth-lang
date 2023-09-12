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

        foreach (MethodDefNode methodDef in classNode.Scope.Statements.OfType<MethodDefNode>())
        {
            DefineMethod(compiler, @class, methodDef);
        }

        foreach (MethodDefNode methodDef in classNode.Scope.Statements.OfType<MethodDefNode>())
        {
            ConvertMethod(compiler, @class, methodDef);
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

    public static void ConvertMethod(CompilerContext compiler, Class @class, MethodDefNode methodDef)
    {
        @class.Functions.TryGetValue(methodDef.Name, out Function func);
        func.OpeningScope = new Scope(func.LLVMFunc.AppendBasicBlock(""));
        compiler.Builder.PositionAtEnd(func.OpeningScope.LLVMBlock);

        foreach (ParameterNode param in methodDef.Params)
        {
            var type = DefToLLVMType(compiler, param.Type, param.TypeRef);
            func.OpeningScope.Variables.Add(param.Name,
                new Variable(compiler.Builder.BuildAlloca(type,
                    param.Name),
                type,
                PrivacyType.Local,
                false));
        }

        ConvertScope(compiler, @class, func.OpeningScope, methodDef.ExecutionBlock);
    }

    public static void ConvertScope(CompilerContext compiler, Class @class, Scope scope, ScopeNode scopeNode)
    {
        compiler.Builder.PositionAtEnd(scope.LLVMBlock);

        foreach (StatementNode statement in scopeNode.Statements)
        {
            if (statement is FieldNode fieldDef)
            {
                var type = DefToLLVMType(compiler, fieldDef.Type, fieldDef.TypeRef);
                scope.Variables.Add(fieldDef.Name,
                    new Variable(compiler.Builder.BuildAlloca(type,
                        fieldDef.Name),
                    type,
                    fieldDef.Privacy,
                    fieldDef.IsConstant));
            }
            else if (statement is ScopeNode newScopeNode)
            {
                var newScope = new Scope(scope.LLVMBlock.InsertBasicBlock(""));
                newScope.Variables = scope.Variables;
                ConvertScope(compiler, @class, newScope, newScopeNode);
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
                if (binaryOp.Left is VariableRefNode varRef)
                {
                    var @var = ResolveVariableRef(compiler, scope, varRef);
                    return compiler.Builder.BuildStore(CompileExpression(compiler, scope, binaryOp.Right), @var.LLVMVariable);
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
        else if (exprNode is VariableRefNode varRef)
        {
            var @var = ResolveVariableRef(compiler, scope, varRef);
            return compiler.Builder.BuildLoad2(@var.LLVMType, @var.LLVMVariable);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static Variable ResolveVariableRef(CompilerContext compiler, Scope scope, VariableRefNode varRef)
    {
        if (varRef.IsLocalVar)
        {
            scope.Variables.TryGetValue(varRef.Name, out Variable @var);

            if (@var == null)
            {
                throw new NotImplementedException();
            }

            return @var;
        }
        else
        {
            throw new NotImplementedException();
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
