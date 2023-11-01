using LLVMSharp.Interop;
using Moth.AST;
using Moth.AST.Node;
using Moth.LLVM.Data;

namespace Moth.LLVM; //TODO: allow compilation of generic classes

public static class LLVMCompiler
{
    public static void Compile(CompilerContext compiler, ScriptAST[] scripts)
    {
        foreach (var script in scripts)
        {
            foreach (var @class in script.ClassNodes)
            {
                if (@class is GenericClassNode genericClass)
                {
                    compiler.GenericClassTemplates.Add(genericClass.Name, genericClass);
                }
                else
                {
                    DefineClass(compiler, @class);
                }
            }
        }

        foreach (var script in scripts)
        {
            foreach (var classNode in script.ClassNodes)
            {
                if (classNode is not GenericClassNode)
                {
                    var @class = compiler.GetClass(classNode.Name);
                    
                    foreach (var funcDefNode in classNode.Scope.Statements.OfType<FuncDefNode>())
                    {
                        DefineFunction(compiler, funcDefNode, @class);
                    }
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
                if (@class is not GenericClassNode)
                {
                    CompileClass(compiler, @class);
                }
            }
        }

        foreach (var script in scripts)
        {
            foreach (var classNode in script.ClassNodes)
            {
                if (classNode is not GenericClassNode)
                {
                    var @class = compiler.GetClass(classNode.Name);

                    foreach (var funcDefNode in classNode.Scope.Statements.OfType<FuncDefNode>())
                    {
                        CompileFunction(compiler, funcDefNode, @class);
                    }
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
        List<LLVMTypeRef> lLVMTypes = new List<LLVMTypeRef>();
        var @class = compiler.GetClass(classNode.Name);
        uint index = 0;

        foreach (FieldDefNode field in classNode.Scope.Statements.OfType<FieldDefNode>())
        {
            Type fieldType = ResolveTypeRef(compiler, field.TypeRef);

            lLVMTypes.Add(fieldType.LLVMType);
            @class.Fields.Add(field.Name, new Field(field.Name, index, fieldType, field.Privacy));
            index++;
        }

        @class.Type.LLVMType.StructSetBody(lLVMTypes.ToArray(), false);
    }

    public static void DefineFunction(CompilerContext compiler, FuncDefNode funcDefNode, Class @class = null)
    {
        int index = 0;
        List<Parameter> @params = new List<Parameter>();
        List<Type> paramTypes = new List<Type>();
        List<LLVMTypeRef> paramLLVMTypes = new List<LLVMTypeRef>();

        if (@class != null && funcDefNode.Privacy != PrivacyType.Static)
        {
            paramLLVMTypes.Add(LLVMTypeRef.CreatePointer(@class.Type.LLVMType, 0));
            index++;
        }

        foreach (ParameterNode paramNode in funcDefNode.Params)
        {
            Type paramType = ResolveParameter(compiler, paramNode);
            paramNode.TypeRef.Name = UnVoid(paramNode.TypeRef);
            @params.Add(new Parameter(index, paramNode.Name, paramType));
            paramTypes.Add(paramType);
            paramLLVMTypes.Add(paramType.LLVMType);
            index++;
        }

        Signature sig = new Signature(funcDefNode.Name, paramTypes.ToArray(), funcDefNode.IsVariadic);
        string funcName = funcDefNode.Name == Reserved.Main || funcDefNode.Privacy == PrivacyType.Foreign
            ? funcDefNode.Name
            : sig.ToString();
        
        if (@class != null)
        {
            funcName = $"{@class.Name}.{funcName}";
        }

        Type returnType = ResolveTypeRef(compiler, funcDefNode.ReturnTypeRef);
        LLVMTypeRef lLVMFuncType = LLVMTypeRef.CreateFunction(returnType.LLVMType, paramLLVMTypes.ToArray(), funcDefNode.IsVariadic);
        LLVMValueRef lLVMFunc = compiler.Module.AddFunction(funcName, lLVMFuncType);
        Function func = new Function(funcDefNode.Name,
            lLVMFunc,
            lLVMFuncType,
            returnType,
            funcDefNode.Privacy,
            @class,
            @params,
            funcDefNode.IsVariadic);

        if (@class != null)
        {
            if (func.Privacy == PrivacyType.Static)
            {
                @class.StaticMethods.Add(sig, func);
            }
            else
            {
                @class.Methods.Add(sig, func);
            }
        }
        else
        {
            compiler.GlobalFunctions.Add(sig, func);
        }

        foreach (var attribute in funcDefNode.Attributes)
        {
            ResolveAttribute(compiler, func, attribute);
        }
    }

    public static string UnVoid(TypeRefNode typeRef)
    {
        string typeName = typeRef.Name;

        if (typeRef.Name == Reserved.Void && typeRef.PointerDepth > 0)
        {
            typeName = Reserved.Char;
        }

        return typeName;
    }

    public static void CompileFunction(CompilerContext compiler, FuncDefNode funcDefNode, Class @class = null)
    {
        Function func;
        List<Type> paramTypes = new List<Type>();

        foreach (var param in funcDefNode.Params)
        {
            paramTypes.Add(ResolveParameter(compiler, param));
        }

        Signature sig = new Signature(funcDefNode.Name, paramTypes.ToArray());

        if (funcDefNode.Privacy == PrivacyType.Foreign && funcDefNode.ExecutionBlock == null)
        {
            return;
        }
        else if (@class != null && funcDefNode.Privacy != PrivacyType.Static
            && @class.Methods.TryGetValue(sig, out func))
        {
            // Keep empty
        }
        else if (@class != null && funcDefNode.Privacy == PrivacyType.Static
            && @class.StaticMethods.TryGetValue(sig, out func))
        {
            // Keep empty
        }
        else if (compiler.GlobalFunctions.TryGetValue(sig, out func))
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
            if (func.ReturnType.Class != func.OwnerClass)
            {
                throw new Exception($"Init method does not return the same type as its owner class (\"{func.OwnerClass.Name}\").");
            }

            var @new = new Variable(Reserved.Self,
                compiler.Builder.BuildMalloc(func.OwnerClass.Type.LLVMType, Reserved.Self), //TODO: malloc or alloc?
                func.OwnerClass.Type);

            func.OpeningScope.Variables.Add(@new.Name, @new);

            foreach (Field field in @new.Type.Class.Fields.Values)
            {
                var lLVMField = compiler.Builder.BuildStructGEP2(@new.Type.LLVMType, @new.LLVMVariable, field.FieldIndex);
                var zeroedVal = lLVMField.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind
                    ? LLVMValueRef.CreateConstPointerNull(field.Type.LLVMType)
                    : LLVMValueRef.CreateConstNull(field.Type.LLVMType);

                compiler.Builder.BuildStore(zeroedVal, lLVMField);
            }
        }

        foreach (Parameter param in compiler.CurrentFunction.Params)
        {
            var paramAsVar = compiler.Builder.BuildAlloca(param.Type.LLVMType, param.Name);
            compiler.Builder.BuildStore(func.LLVMFunc.Params[param.ParamIndex], paramAsVar);
            func.OpeningScope.Variables.Add(param.Name,
                new Variable(param.Name,
                    paramAsVar, //TODO: maybe failing?
                    param.Type));
        }

        if (!CompileScope(compiler, func.OpeningScope, funcDefNode.ExecutionBlock))
        {
            throw new Exception("Function is not guaranteed to return.");
        }
    }

    public static Type ResolveParameter(CompilerContext compiler, ParameterNode param)
    {
        var type = ResolveTypeRef(compiler, param.TypeRef);

        if (param.RequireRefType)
        {
            var newType = new PtrType(type, LLVMTypeRef.CreatePointer(type.LLVMType, 0), type.Class);
            return new RefType(newType, LLVMTypeRef.CreatePointer(newType.LLVMType, 0), newType.Class);
        }
        else
        {
            return type;
        }
    }

    public static void DefineConstant(CompilerContext compiler, FieldDefNode constDef, Class @class = null)
    {
        Type constType = ResolveTypeRef(compiler, constDef.TypeRef);
        var constVal = compiler.Module.AddGlobal(constType.LLVMType, constDef.Name);

        if (@class != null)
        {
            @class.Constants.Add(constDef.Name, new Constant(constType, constVal));
        }
        else
        {
            compiler.GlobalConstants.Add(constDef.Name, new Constant(constType, constVal));
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

    public static Type ResolveTypeRef(CompilerContext compiler, TypeRefNode typeRef)
    {
        Type type; //TODO: is silly and borked???

        if (typeRef is GenericTypeRefNode genTypeRef)
        {
            throw new NotImplementedException();
        }
        else
        {
            var @class = compiler.GetClass(UnVoid(typeRef));
            type = new Type(@class.Type.LLVMType, @class);
        }

        int index = 0;

        while (index < typeRef.PointerDepth)
        {
            type = new PtrType(type, LLVMTypeRef.CreatePointer(type.LLVMType, 0), type.Class);
            index++;
        }

        return type;
    }

    public static ValueContext CompileExpression(CompilerContext compiler, Scope scope, ExpressionNode expr)
    {
        if (expr is BinaryOperationNode binaryOp)
        {
            if (binaryOp.Type == OperationType.Assignment)
            {
                return CompileAssignment(compiler, scope, binaryOp);
            }
            else if (binaryOp.Type == OperationType.Cast)
            {
                return CompileCast(compiler, scope, binaryOp);
            }
            else
            {
                return CompileOperation(compiler, scope, binaryOp);
            }
        }
        else if (expr is LocalDefNode localDef)
        {
            return CompileLocal(compiler, scope, localDef);
        }
        else if (expr is InlineIfNode @if)
        {
            var condition = CompileExpression(compiler, scope, @if.Condition);
            var then = compiler.CurrentFunction.LLVMFunc.AppendBasicBlock("then");
            var @else = compiler.CurrentFunction.LLVMFunc.AppendBasicBlock("else");
            var @continue = compiler.CurrentFunction.LLVMFunc.AppendBasicBlock("continue");

            //then
            compiler.Builder.PositionAtEnd(then);
            var thenVal = CompileExpression(compiler, scope, @if.Then);

            //else
            compiler.Builder.PositionAtEnd(@else);
            var elseVal = CompileExpression(compiler, scope, @if.Else);

            //prior
            compiler.Builder.PositionAtEnd(scope.LLVMBlock);
            var result = compiler.Builder.BuildAlloca(thenVal.Type.LLVMType, "result");
            compiler.Builder.BuildCondBr(SafeLoad(compiler, condition), then, @else);

            //then
            compiler.Builder.PositionAtEnd(then);
            compiler.Builder.BuildStore(SafeLoad(compiler, thenVal), result);
            compiler.Builder.BuildBr(@continue);

            //else
            compiler.Builder.PositionAtEnd(@else);
            compiler.Builder.BuildStore(SafeLoad(compiler, elseVal), result);
            compiler.Builder.BuildBr(@continue);

            //continue
            compiler.Builder.PositionAtEnd(@continue);
            scope.LLVMBlock = @continue;

            if (thenVal.Type.Class.Name != elseVal.Type.Class.Name)
            {
                throw new Exception("Then and else statements of inline if are not of the same type.");
            }

            return new ValueContext(WrapAsRef(thenVal.Type), result);
        }
        else if (expr is ConstantNode constNode)
        {
            return CompileLiteral(compiler, scope, constNode);
        }
        else if (expr is InverseNode inverse)
        {
            var @ref = CompileRef(compiler, scope, inverse.Value);
            return new ValueContext(@ref.Type,
                compiler.Builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ,
                    SafeLoad(compiler, @ref),
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0)));
        }
        else if (expr is IncrementVarNode incrementVar)
        {
            var @ref = CompileRef(compiler, scope, incrementVar.RefNode);
            var valToAdd = LLVMValueRef.CreateConstInt(@ref.Type.Class.Type.LLVMType, 1); //TODO: float compat?
            compiler.Builder.BuildStore(compiler.Builder.BuildAdd(SafeLoad(compiler, @ref), valToAdd), @ref.LLVMValue);
            return new ValueContext(WrapAsRef(@ref.Type),
                @ref.LLVMValue);
        }
        else if (expr is DecrementVarNode decrementVar)
        {
            var @ref = CompileRef(compiler, scope, decrementVar.RefNode);
            var valToSub = LLVMValueRef.CreateConstInt(@ref.Type.Class.Type.LLVMType, 1); //TODO: float compat?
            compiler.Builder.BuildStore(compiler.Builder.BuildSub(SafeLoad(compiler, @ref), valToSub), @ref.LLVMValue);
            return new ValueContext(WrapAsRef(@ref.Type),
                @ref.LLVMValue);
        }
        else if (expr is AsReferenceNode asReference)
        {
            var value = CompileExpression(compiler, scope, asReference.Value);
            var newVal = compiler.Builder.BuildAlloca(value.Type.LLVMType);
            compiler.Builder.BuildStore(value.LLVMValue, newVal);
            return new ValueContext(new PtrType(value.Type, LLVMTypeRef.CreatePointer(value.Type.LLVMType, 0), value.Type.Class), newVal);
        }
        else if (expr is DeReferenceNode deReference)
        {
            var value = CompileExpression(compiler, scope, deReference.Value);
            
            //if (value.Type is RefType @ref)
            //{
            //    value = new ValueContext(@ref.BaseType, compiler.Builder.BuildLoad2(@ref.BaseType.LLVMType, value.LLVMValue));
            //}

            if (value.Type is BasedType bType)
            {
                return new ValueContext(bType.BaseType, compiler.Builder.BuildLoad2(value.Type.LLVMType, value.LLVMValue));
            }
            else
            {
                throw new Exception("Attempted to load a non-pointer.");
            }
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

    public static ValueContext CompileLiteral(CompilerContext compiler, Scope scope, ConstantNode constNode)
    {
        if (constNode.Value is string str)
        {
            var @class = compiler.GetClass(Reserved.Char);
            var constStr = compiler.Context.GetConstString(str, false);
            var global = compiler.Module.AddGlobal(constStr.TypeOf, "");
            global.Initializer = constStr;
            return new ValueContext(new PtrType(@class.Type, LLVMTypeRef.CreatePointer(@class.Type.LLVMType, 0), @class), global);
        }
        else if (constNode.Value is bool @bool)
        {
            var @class = compiler.GetClass(Reserved.Bool);
            return new ValueContext(@class.Type, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, (ulong)(@bool ? 1 : 0)));
        }
        else if (constNode.Value is int i32)
        {
            var @class = compiler.GetClass(Reserved.SignedInt32);
            return new ValueContext(@class.Type, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)i32, true));
        }
        else if (constNode.Value is float f32)
        {
            var @class = compiler.GetClass(Reserved.Float32);
            return new ValueContext(@class.Type, LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, f32));
        }
        else if (constNode.Value is char ch)
        {
            var @class = compiler.GetClass(Reserved.Char);
            return new ValueContext(@class.Type, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, (ulong)ch));
        }
        else if (constNode.Value == null)
        {
            var @class = compiler.GetClass(Reserved.Char);
            return new ValueContext(@class.Type, LLVMValueRef.CreateConstNull(@class.Type.LLVMType));
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static ValueContext CompileOperation(CompilerContext compiler, Scope scope, BinaryOperationNode binaryOp)
    {
        var left = CompileExpression(compiler, scope, binaryOp.Left);
        var right = CompileExpression(compiler, scope, binaryOp.Right);

        if (binaryOp.Type == OperationType.Exponential
            && (right.Type.Class is Float
                || right.Type.Class is Int)
            && (left.Type.Class is Float
                || left.Type.Class is Int))
        {
            return CompilePow(compiler, left, right);
        }
        else if (left.Type.Class.Name == right.Type.Class.Name
            || binaryOp.Type == OperationType.Equal
            || binaryOp.Type == OperationType.NotEqual)
        {
            LLVMValueRef leftVal;
            LLVMValueRef rightVal;
            LLVMValueRef builtVal;

            leftVal = SafeLoad(compiler, left);
            rightVal = SafeLoad(compiler, right);

            switch (binaryOp.Type)
            {
                case OperationType.Addition:
                    if (left.Type.Class is Float)
                    {
                        builtVal = compiler.Builder.BuildFAdd(leftVal, rightVal);
                    }
                    else
                    {
                        builtVal = compiler.Builder.BuildAdd(leftVal, rightVal);
                    }

                    break;
                case OperationType.Subtraction:
                    if (left.Type.Class is Float)
                    {
                        builtVal = compiler.Builder.BuildFSub(leftVal, rightVal);
                    }
                    else
                    {
                        builtVal = compiler.Builder.BuildSub(leftVal, rightVal);
                    }

                    break;
                case OperationType.Multiplication:
                    if (left.Type.Class is Float)
                    {
                        builtVal = compiler.Builder.BuildFMul(leftVal, rightVal);
                    }
                    else
                    {
                        builtVal = compiler.Builder.BuildMul(leftVal, rightVal);
                    }

                    break;
                case OperationType.Division:
                    if (left.Type.Class is Float)
                    {
                        builtVal = compiler.Builder.BuildFDiv(leftVal, rightVal);
                    }
                    else if (left.Type.Class is UnsignedInt)
                    {
                        builtVal = compiler.Builder.BuildUDiv(leftVal, rightVal);
                    }
                    else if (left.Type.Class is SignedInt)
                    {
                        builtVal = compiler.Builder.BuildSDiv(leftVal, rightVal);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    break;
                case OperationType.Modulo:
                    if (left.Type.Class is Float)
                    {
                        builtVal = compiler.Builder.BuildFRem(leftVal, rightVal);
                    }
                    else if (left.Type.Class is UnsignedInt)
                    {
                        builtVal = compiler.Builder.BuildURem(leftVal, rightVal);
                    }
                    else if (left.Type.Class is SignedInt)
                    {
                        builtVal = compiler.Builder.BuildSRem(leftVal, rightVal);
                    }
                    else
                    {
                        throw new NotImplementedException();
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
                    else if (left.Type.Class is Float)
                    {
                        builtVal = compiler.Builder.BuildFCmp(binaryOp.Type == OperationType.Equal
                            ? LLVMRealPredicate.LLVMRealOEQ
                            : LLVMRealPredicate.LLVMRealUNE,
                            leftVal, rightVal);
                    }
                    else if (left.Type.Class is Int)
                    {
                        builtVal = compiler.Builder.BuildICmp(binaryOp.Type == OperationType.Equal
                            ? LLVMIntPredicate.LLVMIntEQ
                            : LLVMIntPredicate.LLVMIntNE,
                            leftVal, rightVal);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    break;
                case OperationType.GreaterThan:
                case OperationType.GreaterThanOrEqual:
                case OperationType.LesserThan:
                case OperationType.LesserThanOrEqual:
                    if (left.Type.Class is Float)
                    {
                        builtVal = compiler.Builder.BuildFCmp(binaryOp.Type switch
                        {
                            OperationType.GreaterThan => LLVMRealPredicate.LLVMRealOGT,
                            OperationType.GreaterThanOrEqual => LLVMRealPredicate.LLVMRealOGE,
                            OperationType.LesserThan => LLVMRealPredicate.LLVMRealOLT,
                            OperationType.LesserThanOrEqual => LLVMRealPredicate.LLVMRealOLE,
                            _ => throw new NotImplementedException(),
                        }, leftVal, rightVal);
                    }
                    else if (left.Type.Class is UnsignedInt)
                    {
                        builtVal = compiler.Builder.BuildICmp(binaryOp.Type switch
                        {
                            OperationType.GreaterThan => LLVMIntPredicate.LLVMIntUGT,
                            OperationType.GreaterThanOrEqual => LLVMIntPredicate.LLVMIntUGE,
                            OperationType.LesserThan => LLVMIntPredicate.LLVMIntULT,
                            OperationType.LesserThanOrEqual => LLVMIntPredicate.LLVMIntULE,
                            _ => throw new NotImplementedException(),
                        }, leftVal, rightVal);
                    }
                    else if (left.Type.Class is SignedInt)
                    {
                        builtVal = compiler.Builder.BuildICmp(binaryOp.Type switch
                        {
                            OperationType.GreaterThan => LLVMIntPredicate.LLVMIntSGT,
                            OperationType.GreaterThanOrEqual => LLVMIntPredicate.LLVMIntSGE,
                            OperationType.LesserThan => LLVMIntPredicate.LLVMIntSLT,
                            OperationType.LesserThanOrEqual => LLVMIntPredicate.LLVMIntSLE,
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

            return new ValueContext(left.Type is RefType @ref ? @ref.BaseType : left.Type, builtVal);
        }
        else
        {
            throw new Exception($"Operation cannot be done with operands of types \"{left.Type.Class.Name}\" "
                + $"and \"{right.Type.Class.Name}\"!");
        }
    }

    public static ValueContext CompileCast(CompilerContext compiler, Scope scope, BinaryOperationNode binaryOp)
    {
        if (binaryOp.Left is not TypeRefNode left)
        {
            throw new Exception("Cast destination is invalid.");
        }

        var destClass = compiler.GetClass(UnVoid(left));
        var right = CompileExpression(compiler, scope, binaryOp.Right);
        Type destType = ResolveTypeRef(compiler, left);
        LLVMValueRef builtVal;

        if (destClass is Int)
        {
            if (right.Type.Class is Int)
            {
                builtVal = compiler.Builder.BuildIntCast(SafeLoad(compiler, right), destType.LLVMType);
            }
            else if (right.Type.Class is Float)
            {
                if (destClass is UnsignedInt)
                {
                    builtVal = compiler.Builder.BuildFPToUI(SafeLoad(compiler, right), destType.LLVMType);
                }
                else if (destClass is SignedInt)
                {
                    builtVal = compiler.Builder.BuildFPToSI(SafeLoad(compiler, right), destType.LLVMType);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else if (destClass is Float)
        {
            if (right.Type.Class is Float)
            {
                builtVal = compiler.Builder.BuildFPCast(SafeLoad(compiler, right), destType.LLVMType);
            }
            else if (right.Type.Class is Int)
            {
                if (right.Type.Class is UnsignedInt)
                {
                    builtVal = compiler.Builder.BuildUIToFP(SafeLoad(compiler, right), destType.LLVMType);
                }
                else if (right.Type.Class is SignedInt)
                {
                    builtVal = compiler.Builder.BuildSIToFP(SafeLoad(compiler, right), destType.LLVMType);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else
        {
            builtVal = compiler.Builder.BuildCast(LLVMOpcode.LLVMBitCast,
                SafeLoad(compiler, right),
                destType.LLVMType);
        }

        return new ValueContext(destType, builtVal);
    }

    public static ValueContext CompilePow(CompilerContext compiler, ValueContext left, ValueContext right)
    {
        Class i16 = compiler.GetClass(Reserved.SignedInt16);
        Class i32 = compiler.GetClass(Reserved.SignedInt32);
        Class f32 = compiler.GetClass(Reserved.Float32);
        Class f64 = compiler.GetClass(Reserved.Float64);
        
        LLVMValueRef val;
        string intrinsic;
        var destType = LLVMTypeRef.Float;

        if (left.Type.Class is Int)
        {
            if (left.Type.Class.Name == Reserved.UnsignedInt64
                || left.Type.Class.Name == Reserved.SignedInt64)
            {
                destType = LLVMTypeRef.Double;
            }

            if (left.Type.Class is SignedInt)
            {
                val = compiler.Builder.BuildSIToFP(SafeLoad(compiler, left), destType); //TODO: not appearing in the module's IR?
            }
            else if (left.Type.Class is UnsignedInt)
            {
                val = compiler.Builder.BuildUIToFP(SafeLoad(compiler, left), destType);
            }
            else
            {
                throw new NotImplementedException();
            }

            left = new ValueContext(val.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind ? f64.Type : f32.Type, val);
        }
        else if (left.Type.Class is Float
            && left.Type.Class.Name != Reserved.Float64)
        {
            val = compiler.Builder.BuildFPCast(SafeLoad(compiler, left), destType);
            left = new ValueContext(val.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind ? f64.Type : f32.Type, val);
        }
        else
        {
            throw new NotImplementedException();
        }

        if (right.Type.Class is Float)
        {
            if (left.Type.Class.Name == Reserved.Float64)
            {
                if (right.Type.Class.Name != Reserved.Float64)
                {
                    val = compiler.Builder.BuildFPCast(right.LLVMValue, LLVMTypeRef.Double);
                    right = new ValueContext(f64.Type, val);
                }

                intrinsic = "llvm.pow.f64";
            }
            else
            {
                if (right.Type.Class.Name != Reserved.Float32)
                {
                    val = compiler.Builder.BuildFPCast(right.LLVMValue, LLVMTypeRef.Float);
                    right = new ValueContext(f32.Type, val);
                }

                intrinsic = "llvm.pow.f32";
            }
        }
        else if (right.Type.Class is Int)
        {
            if (left.Type.Class.Name == Reserved.Float64)
            {
                if (right.Type.Class.Name != Reserved.SignedInt16
                    && right.Type.Class.Name != Reserved.UnsignedInt16)
                {
                    val = compiler.Builder.BuildIntCast(right.LLVMValue, LLVMTypeRef.Int16);
                    right = new ValueContext(i16.Type, val);
                }

                intrinsic = "llvm.powi.f64.i16";
            }
            else
            {
                if (right.Type.Class.Name != Reserved.SignedInt32
                    && right.Type.Class.Name != Reserved.UnsignedInt32)
                {
                    val = compiler.Builder.BuildIntCast(right.LLVMValue, LLVMTypeRef.Int32);
                    right = new ValueContext(i32.Type, val);
                }

                intrinsic = "llvm.powi.f32.i32";
            }
        }
        else
        {
            throw new NotImplementedException();
        }

        var func = compiler.GetIntrinsic(intrinsic);
        return new ValueContext(left.Type,
                    compiler.Builder.BuildCall2(func.LLVMFuncType,
                        func.LLVMFunc,
                        new LLVMValueRef[]
                        {
                            SafeLoad(compiler, left),
                            SafeLoad(compiler, right)
                        }, "pow"));
    }

    public static ValueContext CompileAssignment(CompilerContext compiler, Scope scope, BinaryOperationNode binaryOp)
    {
        ValueContext variableAssigned = CompileExpression(compiler, scope, binaryOp.Left);

        if (variableAssigned.Type is not BasedType)
        {
            throw new Exception($"Cannot assign to \"{variableAssigned.LLVMValue.PrintToString()}\" as it is not a pointer.");
        }

        var value = CompileExpression(compiler, scope, binaryOp.Right);
        compiler.Builder.BuildStore(SafeLoad(compiler, value), variableAssigned.LLVMValue);
        return new ValueContext(WrapAsRef(variableAssigned.Type), variableAssigned.LLVMValue);
    }

    public static ValueContext CompileLocal(CompilerContext compiler, Scope scope, LocalDefNode localDef)
    {
        ValueContext value = null;
        Type type;

        if (localDef is InferredLocalDefNode inferredLocalDef)
        {
            value = CompileExpression(compiler, scope, inferredLocalDef.Value);
            type = value.Type;
        }
        else
        {
            type = ResolveTypeRef(compiler, localDef.TypeRef);
        }

        var @var = compiler.Builder.BuildAlloca(type.LLVMType, localDef.Name);
        scope.Variables.Add(localDef.Name, new Variable(localDef.Name, @var, type));

        if (value != null)
        {
            compiler.Builder.BuildStore(SafeLoad(compiler, value), @var);
        }

        return new ValueContext(WrapAsRef(type), @var);
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
                    var self = scope.GetVariable(Reserved.Self);
                    context = new ValueContext(WrapAsRef(self.Type),
                            self.LLVMVariable);
                }
                else
                {
                    context = new ValueContext(WrapAsRef(compiler.CurrentFunction.OwnerClass.Type),
                        compiler.CurrentFunction.LLVMFunc.FirstParam);
                }

                refNode = refNode.Child;
            }
            else if (refNode is TypeRefNode typeRef)
            {
                var @class = compiler.GetClass(UnVoid(typeRef));
                refNode = refNode.Child;

                if (refNode is FuncCallNode methodCall)
                {
                    context = CompileFuncCall(compiler, context, scope, methodCall, @class);
                    refNode = refNode.Child;
                }
                else
                {
                    var field = @class.GetStaticField(refNode.Name);
                    throw new NotImplementedException();
                }
            }
            else if (refNode is FuncCallNode methodCall)
            {
                context = CompileFuncCall(compiler, context, scope, methodCall);
                refNode = refNode.Child;
            }
            else if (refNode is IndexAccessNode indexAccess)
            {
                context = CompileVarRef(compiler, context, scope, refNode);
                context = new ValueContext(WrapAsRef(context.Type.Class.Type),
                    compiler.Builder.BuildInBoundsGEP2(context.Type.Class.Type.LLVMType, SafeLoad(compiler, context), new LLVMValueRef[1]
                    {
                        compiler.Builder.BuildIntCast(SafeLoad(compiler,
                            CompileExpression(compiler, scope, indexAccess.Index)),
                            LLVMTypeRef.Int64)
                    }));
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

    public static ValueContext CompileVarRef(CompilerContext compiler, ValueContext context, Scope scope, RefNode refNode)
    {
        if (context != null)
        {
            var field = context.Type.Class.GetField(refNode.Name);
            var type = context.Type is RefType refType ? refType.BaseType : context.Type;
            return new ValueContext(WrapAsRef(field.Type),
                compiler.Builder.BuildStructGEP2(type.LLVMType,
                    context.LLVMValue,
                    field.FieldIndex));
        }
        else
        {
            if (scope.Variables.TryGetValue(refNode.Name, out Variable @var))
            {
                return new ValueContext(WrapAsRef(@var.Type),
                    @var.LLVMVariable);
            }
            else if (compiler.GlobalConstants.TryGetValue(refNode.Name, out Constant @const))
            {
                return new ValueContext(WrapAsRef(@const.Type),
                    @const.LLVMValue);
            }
            else
            {
                throw new Exception($"Variable \"{refNode.Name}\" does not exist.");
            }
        }
    }

    public static ValueContext CompileFuncCall(CompilerContext compiler, ValueContext context, Scope scope, FuncCallNode methodCall,
        Class staticClass = null)
    {
        List<Type> argTypes = new List<Type>();
        List<LLVMValueRef> args = new List<LLVMValueRef>();
        bool contextWasNull = context == null;
        Function func;

        if (context == null)
        {
            if (compiler.CurrentFunction.OwnerClass != null)
            {
                context = new ValueContext(compiler.CurrentFunction.OwnerClass.Type,
                    compiler.CurrentFunction.LLVMFunc.FirstParam);
            }
        }

        foreach (ExpressionNode arg in methodCall.Arguments)
        {
            var val = CompileExpression(compiler, scope, arg);
            argTypes.Add(val.Type is RefType @ref ? @ref.BaseType : val.Type);
            args.Add(SafeLoad(compiler, val));
        }

        Signature sig = new Signature(methodCall.Name, argTypes.ToArray());

        if (context != null && context.Type.Class.Methods.TryGetValue(sig, out func))
        {
            if (context.Type.LLVMType == func.LLVMFuncType.ParamTypes[0])
            {
                var newArgs = new List<LLVMValueRef>() { context.LLVMValue };
                newArgs.AddRange(args);
                args = newArgs;
            }
            else
            {
                throw new Exception("Attempted to call a method on a different class. !!THIS IS NOT A USER ERROR. REPORT ASAP!!");
            }
        }
        else if (contextWasNull && compiler.GlobalFunctions.TryGetValue(sig, out func))
        {
            // Keep empty
        }
        else if (staticClass != null && staticClass.StaticMethods.TryGetValue(sig, out func))
        {
            // Keep empty
        }
        else
        {
            throw new Exception($"Function \"{methodCall.Name}\" does not exist.");
        }

        return new ValueContext(func.ReturnType,
            compiler.Builder.BuildCall2(func.LLVMFuncType, func.LLVMFunc, args.ToArray()));
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
                    var lLVMFunc = func.LLVMFunc;
                    lLVMFunc.FunctionCallConv = str switch
                    {
                        "cdecl" => 0,
                        _ => throw new Exception("Invalid calling convention!"),
                    };
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
        if (value.Type is RefType @ref && value.Type.Class.Name != Reserved.Void)
        {
            return compiler.Builder.BuildLoad2(@ref.BaseType.LLVMType, value.LLVMValue);
        }
        else
        {
            return value.LLVMValue;
        }
    }

    public static RefType WrapAsRef(Type type)
    {
        if (type is RefType @ref)
        {
            return @ref;
        }

        return new RefType(type, LLVMTypeRef.CreatePointer(type.LLVMType, 0), type.Class);
    }
}