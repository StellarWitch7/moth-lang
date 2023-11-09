using Moth.AST;
using Moth.AST.Node;
using Moth.LLVM.Data;

namespace Moth.LLVM;

public class LLVMCompiler
{
    public string ModuleName { get; }
    public LLVMContextRef Context { get; }
    public LLVMModuleRef Module { get; }
    public LLVMBuilderRef Builder { get; }
    public FuncDictionary GlobalFunctions { get; } = new FuncDictionary();
    public Dictionary<string, Constant> GlobalConstants { get; } = new Dictionary<string, Constant>();
    public Dictionary<string, Class> Classes { get; } = new Dictionary<string, Class>();
    public Dictionary<string, GenericClassNode> GenericClassTemplates { get; } = new Dictionary<string, GenericClassNode>();
    public GenericDictionary GenericClasses { get; } = new GenericDictionary();
    public LLVMFunction? CurrentFunction { get; set; }

    private readonly Logger _logger = new Logger("moth/compiler");
    private readonly Dictionary<string, IntrinsicFunction> _intrinsics = new Dictionary<string, IntrinsicFunction>();

    public LLVMCompiler(string moduleName)
    {
        ModuleName = moduleName;
        Context = LLVMContextRef.Global;
        Builder = Context.CreateBuilder();
        Module = Context.CreateModuleWithName(ModuleName);

        InsertDefaultTypes();
    }

    public LLVMCompiler(string moduleName, IReadOnlyCollection<ScriptAST> scripts) : this(moduleName) => Compile(scripts);

    public IntrinsicFunction GetIntrinsic(string name)
        => _intrinsics.TryGetValue(name, out IntrinsicFunction? func)
            ? func
            : CreateIntrinsic(name);

    public Function GetFunction(Signature sig)
        => GlobalFunctions.TryGetValue(sig, out Function? func)
            ? func
            : throw new Exception($"Function \"{sig}\" does not exist.");

    public Class GetClass(string name)
        => Classes.TryGetValue(name, out Class? @class)
            ? @class
            : throw new Exception($"Class \"{name}\" does not exist.");

    public void Warn(string message) => Log($"Warning: {message}");

    public void Log(string message) => _logger.WriteLine(message);

    public LLVMCompiler Compile(IReadOnlyCollection<ScriptAST> scripts)
    {
        foreach (ScriptAST script in scripts)
        {
            foreach (FieldDefNode constDefNode in script.GlobalConstants)
            {
                DefineConstant(constDefNode);
            }

            foreach (FuncDefNode funcDefNode in script.GlobalFunctions)
            {
                DefineFunction(funcDefNode);
            }

            foreach (ClassNode @class in script.ClassNodes)
            {
                if (@class is GenericClassNode genericClass)
                {
                    GenericClassTemplates.Add(genericClass.Name, genericClass);
                }
                else
                {
                    DefineClass(@class);
                }
            }

            foreach (ClassNode classNode in script.ClassNodes)
            {
                if (classNode is not GenericClassNode)
                {
                    Class @class = GetClass(classNode.Name);

                    foreach (FuncDefNode funcDefNode in classNode.Scope.Statements.OfType<FuncDefNode>())
                    {
                        DefineFunction(funcDefNode, @class);
                    }
                }
            }
        }

        foreach (ScriptAST script in scripts)
        {
            foreach (ClassNode @class in script.ClassNodes)
            {
                if (@class is not GenericClassNode)
                {
                    CompileClass(@class);
                }
            }
        }

        foreach (ScriptAST script in scripts)
        {
            foreach (FuncDefNode funcDefNode in script.GlobalFunctions)
            {
                CompileFunction(funcDefNode);
            }

            foreach (ClassNode classNode in script.ClassNodes)
            {
                if (classNode is not GenericClassNode)
                {
                    Class @class = GetClass(classNode.Name);

                    foreach (FuncDefNode funcDefNode in classNode.Scope.Statements.OfType<FuncDefNode>())
                    {
                        CompileFunction(funcDefNode, @class);
                    }
                }
            }
        }

        return this;
    }

    public void DefineClass(ClassNode classNode)
    {
        LLVMTypeRef newStruct = Context.CreateNamedStruct(classNode.Name);
        var newClass = new Class(classNode.Name, newStruct, classNode.Privacy);
        Classes.Add(classNode.Name, newClass);
        newClass.AddBuiltins(this);
    }

    public void CompileClass(ClassNode classNode)
    {
        var llvmTypes = new List<LLVMTypeRef>();
        Class @class = GetClass(classNode.Name);
        uint index = 0;

        foreach (FieldDefNode field in classNode.Scope.Statements.OfType<FieldDefNode>())
        {
            Type fieldType = ResolveTypeRef(field.TypeRef);
            llvmTypes.Add(fieldType.LLVMType);
            @class.Fields.Add(field.Name, new Field(field.Name, index, fieldType, field.Privacy));
            index++;
        }

        @class.Type.LLVMType.StructSetBody(llvmTypes.AsReadonlySpan(), false);
    }

    public void DefineFunction(FuncDefNode funcDefNode, Class? @class = null)
    {
        int index = 0;
        var @params = new List<Parameter>();
        var paramTypes = new List<Type>();
        var paramLLVMTypes = new List<LLVMTypeRef>();

        if (@class != null && funcDefNode.Privacy != PrivacyType.Static)
        {
            paramLLVMTypes.Add(LLVMTypeRef.CreatePointer(@class.Type.LLVMType, 0));
            index++;
        }

        foreach (ParameterNode paramNode in funcDefNode.Params)
        {
            Type paramType = ResolveParameter(paramNode);
            paramNode.TypeRef.Name = UnVoid(paramNode.TypeRef);
            @params.Add(new Parameter(index, paramNode.Name, paramType));
            paramTypes.Add(paramType);
            paramLLVMTypes.Add(paramType.LLVMType);
            index++;
        }

        var sig = new Signature(funcDefNode.Name, paramTypes, funcDefNode.IsVariadic);
        string funcName = funcDefNode.Name == Reserved.Main || funcDefNode.Privacy == PrivacyType.Foreign
            ? funcDefNode.Name
            : sig.ToString();

        if (@class != null)
        {
            funcName = $"{@class.Name}.{funcName}";
        }

        Type returnType = ResolveTypeRef(funcDefNode.ReturnTypeRef);
        var llvmFuncType = LLVMTypeRef.CreateFunction(returnType.LLVMType, paramLLVMTypes.AsReadonlySpan(), funcDefNode.IsVariadic);
        LLVMValueRef llvmFunc = Module.AddFunction(funcName, llvmFuncType);
        var func = new LLVMFunction(funcDefNode.Name,
            llvmFunc,
            llvmFuncType,
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
            GlobalFunctions.Add(sig, func);
        }

        foreach (AttributeNode attribute in funcDefNode.Attributes)
        {
            ResolveAttribute(func, attribute);
        }
    }

    public string UnVoid(TypeRefNode typeRef)
    {
        string typeName = typeRef.Name;

        if (typeRef.Name == Reserved.Void && typeRef.PointerDepth > 0)
        {
            typeName = Reserved.Char;
        }

        return typeName;
    }

    public void CompileFunction(FuncDefNode funcDefNode, Class? @class = null)
    {
        Function fn;
        var paramTypes = new List<Type>();

        foreach (ParameterNode param in funcDefNode.Params)
        {
            paramTypes.Add(ResolveParameter(param));
        }

        var sig = new Signature(funcDefNode.Name, paramTypes);

        if (funcDefNode.Privacy == PrivacyType.Foreign && funcDefNode.ExecutionBlock == null)
        {
            return;
        }
        else if (@class != null && funcDefNode.Privacy != PrivacyType.Static
            && @class.Methods.TryGetValue(sig, out fn))
        {
            // Keep empty
        }
        else if (@class != null && funcDefNode.Privacy == PrivacyType.Static
            && @class.StaticMethods.TryGetValue(sig, out fn))
        {
            // Keep empty
        }
        else if (GlobalFunctions.TryGetValue(sig, out fn))
        {
            // Keep empty
        }
        else
        {
            throw new Exception($"Cannot compile function {funcDefNode.Name} as it is undefined.");
        }

        if (fn is not LLVMFunction func)
        {
            throw new Exception($"{fn.Name} cannot be compiled.");
        }

        func.OpeningScope = new Scope(func.LLVMFunc.AppendBasicBlock("entry"));
        Builder.PositionAtEnd(func.OpeningScope.LLVMBlock);
        CurrentFunction = func;

        if (funcDefNode.Name == Reserved.Init && funcDefNode.Privacy == PrivacyType.Static)
        {
            if (func.ReturnType.Class != func.OwnerClass)
            {
                throw new Exception($"Init method does not return the same type as its owner class (\"{func.OwnerClass.Name}\").");
            }

            var @new = new Variable(Reserved.Self,
                Builder.BuildMalloc(func.OwnerClass.Type.LLVMType, Reserved.Self), //TODO: malloc or alloc?
                func.OwnerClass.Type);

            func.OpeningScope.Variables.Add(@new.Name, @new);

            foreach (Field field in @new.Type.Class.Fields.Values)
            {
                LLVMValueRef llvmField = Builder.BuildStructGEP2(@new.Type.LLVMType, @new.LLVMVariable, field.FieldIndex);
                var zeroedVal = LLVMValueRef.CreateConstNull(field.Type.LLVMType);

                Builder.BuildStore(zeroedVal, llvmField);
            }
        }

        foreach (Parameter param in CurrentFunction.Params)
        {
            LLVMValueRef paramAsVar = Builder.BuildAlloca(param.Type.LLVMType, param.Name);
            Builder.BuildStore(func.LLVMFunc.Params[param.ParamIndex], paramAsVar);
            func.OpeningScope.Variables.Add(param.Name,
                new Variable(param.Name,
                    paramAsVar,
                    new RefType(param.Type)));
        }

        if (!CompileScope(func.OpeningScope, funcDefNode.ExecutionBlock))
        {
            throw new Exception("Function is not guaranteed to return.");
        }
    }

    public Type ResolveParameter(ParameterNode param)
    {
        Type type = ResolveTypeRef(param.TypeRef);

        return param.RequireRefType
            ? new PtrType(new RefType(type))
            : type;
    }

    public void DefineConstant(FieldDefNode constDef, Class? @class = null)
    {
        Type constType = ResolveTypeRef(constDef.TypeRef);
        LLVMValueRef constVal = Module.AddGlobal(constType.LLVMType, constDef.Name);

        if (@class != null)
        {
            @class.Constants.Add(constDef.Name, new Constant(constType, constVal));
        }
        else
        {
            GlobalConstants.Add(constDef.Name, new Constant(constType, constVal));
        }
    }

    public bool CompileScope(Scope scope, ScopeNode scopeNode)
    {
        Builder.PositionAtEnd(scope.LLVMBlock);

        foreach (StatementNode statement in scopeNode.Statements)
        {
            if (statement is ReturnNode @return)
            {
                if (CurrentFunction == null)
                {
                    throw new Exception("Return is not within a function!");
                }

                if (@return.ReturnValue != null)
                {
                    ValueContext expr = SafeLoad(CompileExpression(scope, @return.ReturnValue));

                    if (expr.Type.Equals(CurrentFunction.ReturnType))
                    {
                        Builder.BuildRet(expr.LLVMValue);
                    }
                    else
                    {
                        throw new Exception($"Return value \"{expr.LLVMValue}\" does not match return type of function "
                            + $"\"{CurrentFunction.Name}\" (\"{CurrentFunction.ReturnType}\").");
                    }
                }
                else
                {
                    Builder.BuildRetVoid();
                }

                return true;
            }
            else if (statement is ScopeNode newScopeNode)
            {
                var newScope = new Scope(CurrentFunction.LLVMFunc.AppendBasicBlock(""))
                {
                    Variables = new Dictionary<string, Variable>(scope.Variables)
                };
                Builder.BuildBr(newScope.LLVMBlock);
                Builder.PositionAtEnd(newScope.LLVMBlock);

                if (CompileScope(newScope, newScopeNode))
                {
                    return true;
                }

                scope.LLVMBlock = CurrentFunction.LLVMFunc.AppendBasicBlock("");
                Builder.BuildBr(scope.LLVMBlock);
                Builder.PositionAtEnd(scope.LLVMBlock);
            }
            else if (statement is WhileNode @while)
            {
                LLVMBasicBlockRef loop = CurrentFunction.LLVMFunc.AppendBasicBlock("loop");
                LLVMBasicBlockRef then = CurrentFunction.LLVMFunc.AppendBasicBlock("then");
                LLVMBasicBlockRef @continue = CurrentFunction.LLVMFunc.AppendBasicBlock("continue");
                Builder.BuildBr(loop);
                Builder.PositionAtEnd(loop);
                ValueContext condition = CompileExpression(scope, @while.Condition);
                Builder.BuildCondBr(SafeLoad(condition).LLVMValue, then, @continue);
                Builder.PositionAtEnd(then);

                var newScope = new Scope(then)
                {
                    Variables = new Dictionary<string, Variable>(scope.Variables)
                };

                if (!CompileScope(newScope, @while.Then))
                {
                    Builder.BuildBr(@loop);
                }

                Builder.PositionAtEnd(@continue);
                scope.LLVMBlock = @continue;
            }
            else if (statement is IfNode @if)
            {
                ValueContext condition = CompileExpression(scope, @if.Condition);
                LLVMBasicBlockRef then = CurrentFunction.LLVMFunc.AppendBasicBlock("then");
                LLVMBasicBlockRef @else = CurrentFunction.LLVMFunc.AppendBasicBlock("else");
                LLVMBasicBlockRef @continue = null;
                bool thenReturned = false;
                bool elseReturned = false;

                Builder.BuildCondBr(SafeLoad(condition).LLVMValue, then, @else);

                //then
                {
                    Builder.PositionAtEnd(then);

                    {
                        var newScope = new Scope(then)
                        {
                            Variables = new Dictionary<string, Variable>(scope.Variables)
                        };

                        if (CompileScope(newScope, @if.Then))
                        {
                            thenReturned = true;
                        }
                        else
                        {
                            if (@continue == null)
                            {
                                @continue = CurrentFunction.LLVMFunc.AppendBasicBlock("continue");
                            }

                            Builder.BuildBr(@continue);
                        }
                    }
                }

                //else
                {
                    Builder.PositionAtEnd(@else);

                    var newScope = new Scope(@else)
                    {
                        Variables = new Dictionary<string, Variable>(scope.Variables)
                    };

                    if (@if.Else != null && CompileScope(newScope, @if.Else))
                    {
                        elseReturned = true;
                    }
                    else
                    {
                        if (@continue == null)
                        {
                            @continue = CurrentFunction.LLVMFunc.AppendBasicBlock("continue");
                        }

                        Builder.BuildBr(@continue);
                    }
                }

                if (thenReturned && elseReturned)
                {
                    return true;
                }
                else
                {
                    Builder.PositionAtEnd(@continue);
                    scope.LLVMBlock = @continue;
                }
            }
            else if (statement is ExpressionNode exprNode)
            {
                CompileExpression(scope, exprNode);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        return false;
    }

    public Type ResolveTypeRef(TypeRefNode typeRef)
    {
        Type type; //TODO: is silly and borked???

        if (typeRef is GenericTypeRefNode)
        {
            throw new NotImplementedException();
        }
        else
        {
            Class @class = GetClass(UnVoid(typeRef));
            type = new Type(@class.Type.LLVMType, @class, TypeKind.Class);
        }

        int index = 0;

        while (index < typeRef.PointerDepth)
        {
            type = new PtrType(type);
            index++;
        }

        return type;
    }

    public ValueContext CompileExpression(Scope scope, ExpressionNode expr)
    {
        if (expr is BinaryOperationNode binaryOp)
        {
            return binaryOp.Type == OperationType.Assignment
                ? CompileAssignment(scope, binaryOp)
                : binaryOp.Type == OperationType.Cast
                    ? CompileCast(scope, binaryOp)
                    : CompileOperation(scope, binaryOp);
        }
        else if (expr is LocalDefNode localDef)
        {
            return CompileLocal(scope, localDef);
        }
        else if (expr is InlineIfNode @if)
        {
            ValueContext condition = CompileExpression(scope, @if.Condition);
            LLVMBasicBlockRef then = CurrentFunction.LLVMFunc.AppendBasicBlock("then");
            LLVMBasicBlockRef @else = CurrentFunction.LLVMFunc.AppendBasicBlock("else");
            LLVMBasicBlockRef @continue = CurrentFunction.LLVMFunc.AppendBasicBlock("continue");

            //then
            Builder.PositionAtEnd(then);
            ValueContext thenVal = CompileExpression(scope, @if.Then);

            //else
            Builder.PositionAtEnd(@else);
            ValueContext elseVal = CompileExpression(scope, @if.Else);

            //prior
            Builder.PositionAtEnd(scope.LLVMBlock);
            LLVMValueRef result = Builder.BuildAlloca(thenVal.Type.LLVMType, "result");
            Builder.BuildCondBr(SafeLoad(condition).LLVMValue, then, @else);

            //then
            Builder.PositionAtEnd(then);
            Builder.BuildStore(SafeLoad(thenVal).LLVMValue, result);
            Builder.BuildBr(@continue);

            //else
            Builder.PositionAtEnd(@else);
            Builder.BuildStore(SafeLoad(elseVal).LLVMValue, result);
            Builder.BuildBr(@continue);

            //continue
            Builder.PositionAtEnd(@continue);
            scope.LLVMBlock = @continue;

            return thenVal.Type.Class.Name != elseVal.Type.Class.Name
                ? throw new Exception("Then and else statements of inline if are not of the same type.")
                : new ValueContext(WrapAsRef(thenVal.Type), result);
        }
        else if (expr is ConstantNode constNode)
        {
            return CompileLiteral(constNode);
        }
        else if (expr is InverseNode inverse)
        {
            ValueContext @ref = CompileRef(scope, inverse.Value);
            return new ValueContext(@ref.Type,
                Builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ,
                    SafeLoad(@ref).LLVMValue,
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0)));
        }
        else if (expr is IncrementVarNode incrementVar)
        {
            ValueContext @ref = CompileRef(scope, incrementVar.RefNode);
            var valToAdd = LLVMValueRef.CreateConstInt(@ref.Type.Class.Type.LLVMType, 1); //TODO: float compat?
            Builder.BuildStore(Builder.BuildAdd(SafeLoad(@ref).LLVMValue, valToAdd), @ref.LLVMValue);
            return new ValueContext(WrapAsRef(@ref.Type),
                @ref.LLVMValue);
        }
        else if (expr is DecrementVarNode decrementVar)
        {
            ValueContext @ref = CompileRef(scope, decrementVar.RefNode);
            var valToSub = LLVMValueRef.CreateConstInt(@ref.Type.Class.Type.LLVMType, 1); //TODO: float compat?
            Builder.BuildStore(Builder.BuildSub(SafeLoad(@ref).LLVMValue, valToSub), @ref.LLVMValue);
            return new ValueContext(WrapAsRef(@ref.Type),
                @ref.LLVMValue);
        }
        else if (expr is AsReferenceNode asReference)
        {
            ValueContext value = CompileExpression(scope, asReference.Value);
            LLVMValueRef newVal = Builder.BuildAlloca(value.Type.LLVMType);
            Builder.BuildStore(value.LLVMValue, newVal);
            return new ValueContext(new PtrType(value.Type), newVal);
        }
        else if (expr is DeReferenceNode deReference)
        {
            ValueContext value = SafeLoad(CompileExpression(scope, deReference.Value));

            return value.Type is PtrType ptrType
                ? new ValueContext(ptrType.BaseType, Builder.BuildLoad2(ptrType.BaseType.LLVMType, value.LLVMValue))
                : throw new Exception("Attempted to load a non-pointer.");
        }
        else
        {
            return expr is RefNode @ref
                ? CompileRef(scope, @ref)
                : expr is SubExprNode subExpr ? CompileExpression(scope, subExpr.Expression) : throw new NotImplementedException();
        }
    }

    public ValueContext CompileLiteral(ConstantNode constNode)
    {
        if (constNode.Value is string str)
        {
            Class @class = GetClass(Reserved.Char);
            LLVMValueRef constStr = Context.GetConstString(str, false);
            LLVMValueRef global = Module.AddGlobal(constStr.TypeOf, "litstr");
            global.Initializer = constStr;
            return new ValueContext(new PtrType(@class.Type), global);
        }
        else if (constNode.Value is bool @bool)
        {
            Class @class = GetClass(Reserved.Bool);
            return new ValueContext(@class.Type, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, (ulong)(@bool ? 1 : 0)));
        }
        else if (constNode.Value is int i32)
        {
            Class @class = GetClass(Reserved.SignedInt32);
            return new ValueContext(@class.Type, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)i32, true));
        }
        else if (constNode.Value is float f32)
        {
            Class @class = GetClass(Reserved.Float32);
            return new ValueContext(@class.Type, LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, f32));
        }
        else if (constNode.Value is char ch)
        {
            Class @class = GetClass(Reserved.Char);
            return new ValueContext(@class.Type, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, ch));
        }
        else if (constNode.Value == null)
        {
            Class @class = GetClass(Reserved.Char);
            return new ValueContext(@class.Type, LLVMValueRef.CreateConstNull(@class.Type.LLVMType));
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public ValueContext CompileOperation(Scope scope, BinaryOperationNode binaryOp)
    {
        ValueContext left = SafeLoad(CompileExpression(scope, binaryOp.Left));
        ValueContext right = SafeLoad(CompileExpression(scope, binaryOp.Right));

        if (binaryOp.Type == OperationType.Exponential
            && right.Type.Class is Float or Int
            && left.Type.Class is Float or Int)
        {
            return CompilePow(left, right);
        }
        else if (left.Type.Class.Name == right.Type.Class.Name
            || binaryOp.Type == OperationType.Equal
            || binaryOp.Type == OperationType.NotEqual)
        {
            LLVMValueRef leftVal;
            LLVMValueRef rightVal;
            LLVMValueRef builtVal;
            Type builtType;

            leftVal = left.LLVMValue;
            rightVal = right.LLVMValue;
            builtType = left.Type;

            switch (binaryOp.Type)
            {
                case OperationType.Addition:
                    builtVal = left.Type.Class is Float
                        ? Builder.BuildFAdd(leftVal, rightVal)
                        : Builder.BuildAdd(leftVal, rightVal);
                    break;
                case OperationType.Subtraction:
                    builtVal = left.Type.Class is Float
                        ? Builder.BuildFSub(leftVal, rightVal)
                        : Builder.BuildSub(leftVal, rightVal);
                    break;
                case OperationType.Multiplication:
                    builtVal = left.Type.Class is Float
                        ? Builder.BuildFMul(leftVal, rightVal)
                        : Builder.BuildMul(leftVal, rightVal);
                    break;
                case OperationType.Division:
                    builtVal = left.Type.Class is Float
                        ? Builder.BuildFDiv(leftVal, rightVal)
                        : left.Type.Class is UnsignedInt
                            ? Builder.BuildUDiv(leftVal, rightVal)
                            : left.Type.Class is SignedInt
                                ? Builder.BuildSDiv(leftVal, rightVal)
                                : throw new NotImplementedException();

                    break;
                case OperationType.Modulo:
                    builtVal = left.Type.Class is Float
                        ? Builder.BuildFRem(leftVal, rightVal)
                        : left.Type.Class is UnsignedInt
                            ? Builder.BuildURem(leftVal, rightVal)
                            : left.Type.Class is SignedInt
                                ? Builder.BuildSRem(leftVal, rightVal)
                                : throw new NotImplementedException();

                    break;
                case OperationType.And:
                    builtVal = Builder.BuildAnd(leftVal, rightVal);
                    builtType = UnsignedInt.Bool.Type;
                    break;
                case OperationType.Or:
                    builtVal = Builder.BuildOr(leftVal, rightVal);
                    builtType = UnsignedInt.Bool.Type;
                    break;
                case OperationType.Equal:
                case OperationType.NotEqual:
                    builtVal = right.LLVMValue.IsNull
                        ? binaryOp.Type == OperationType.Equal
                            ? Builder.BuildIsNull(leftVal)
                            : Builder.BuildIsNotNull(leftVal)
                        : left.LLVMValue.IsNull
                            ? binaryOp.Type == OperationType.Equal
                                ? Builder.BuildIsNull(rightVal)
                                : Builder.BuildIsNotNull(rightVal)
                            : left.Type.Class is Float
                                ? Builder.BuildFCmp(binaryOp.Type == OperationType.Equal
                                        ? LLVMRealPredicate.LLVMRealOEQ
                                        : LLVMRealPredicate.LLVMRealUNE,
                                    leftVal, rightVal)
                                : left.Type.Class is Int
                                    ? Builder.BuildICmp(binaryOp.Type == OperationType.Equal
                                            ? LLVMIntPredicate.LLVMIntEQ
                                            : LLVMIntPredicate.LLVMIntNE,
                                        leftVal, rightVal)
                                    : throw new NotImplementedException();

                    builtType = UnsignedInt.Bool.Type;
                    break;
                case OperationType.GreaterThan:
                case OperationType.GreaterThanOrEqual:
                case OperationType.LesserThan:
                case OperationType.LesserThanOrEqual:
                    builtVal = left.Type.Class is Float
                        ? Builder.BuildFCmp(binaryOp.Type switch
                        {
                            OperationType.GreaterThan => LLVMRealPredicate.LLVMRealOGT,
                            OperationType.GreaterThanOrEqual => LLVMRealPredicate.LLVMRealOGE,
                            OperationType.LesserThan => LLVMRealPredicate.LLVMRealOLT,
                            OperationType.LesserThanOrEqual => LLVMRealPredicate.LLVMRealOLE,
                            _ => throw new NotImplementedException(),
                        }, leftVal, rightVal)
                        : left.Type.Class is UnsignedInt
                            ? Builder.BuildICmp(binaryOp.Type switch
                            {
                                OperationType.GreaterThan => LLVMIntPredicate.LLVMIntUGT,
                                OperationType.GreaterThanOrEqual => LLVMIntPredicate.LLVMIntUGE,
                                OperationType.LesserThan => LLVMIntPredicate.LLVMIntULT,
                                OperationType.LesserThanOrEqual => LLVMIntPredicate.LLVMIntULE,
                                _ => throw new NotImplementedException(),
                            }, leftVal, rightVal)
                            : left.Type.Class is SignedInt
                                ? Builder.BuildICmp(binaryOp.Type switch
                                {
                                    OperationType.GreaterThan => LLVMIntPredicate.LLVMIntSGT,
                                    OperationType.GreaterThanOrEqual => LLVMIntPredicate.LLVMIntSGE,
                                    OperationType.LesserThan => LLVMIntPredicate.LLVMIntSLT,
                                    OperationType.LesserThanOrEqual => LLVMIntPredicate.LLVMIntSLE,
                                    _ => throw new NotImplementedException(),
                                }, leftVal, rightVal)
                                : throw new NotImplementedException($"Unimplemented comparison between {left.Type.Class.Name} and " +
                                    $"{right.Type.Class.Name}.");

                    builtType = UnsignedInt.Bool.Type;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return new ValueContext(builtType, builtVal);
        }
        else
        {
            throw new Exception($"Operation cannot be done with operands of types \"{left.Type.Class.Name}\" "
                + $"and \"{right.Type.Class.Name}\"!");
        }
    }

    public ValueContext CompileCast(Scope scope, BinaryOperationNode binaryOp)
    {
        if (binaryOp.Left is not TypeRefNode left)
        {
            throw new Exception($"Cast destination (\"{binaryOp.Left}\") is invalid.");
        }

        ValueContext right = SafeLoad(CompileExpression(scope, binaryOp.Right));
        Type destType = ResolveTypeRef(left);
        LLVMValueRef builtVal = destType.Class is Int
            ? right.Type.Class is Int
                ? destType.Class.Name == Reserved.Bool
                    ? Builder.BuildICmp(LLVMIntPredicate.LLVMIntNE,
                        LLVMValueRef.CreateConstInt(right.Type.LLVMType, 0), right.LLVMValue)
                    : right.Type.Class.Name == Reserved.Bool
                        ? Builder.BuildZExt(right.LLVMValue, destType.LLVMType)
                        : Builder.BuildIntCast(right.LLVMValue, destType.LLVMType)
                : right.Type.Class is Float
                    ? destType.Class is UnsignedInt
                        ? Builder.BuildFPToUI(right.LLVMValue, destType.LLVMType)
                        : destType.Class is SignedInt
                            ? Builder.BuildFPToSI(right.LLVMValue, destType.LLVMType)
                            : throw new NotImplementedException()
                    : throw new NotImplementedException()
            : destType.Class is Float
                ? right.Type.Class is Float
                    ? Builder.BuildFPCast(right.LLVMValue, destType.LLVMType)
                    : right.Type.Class is Int
                        ? right.Type.Class is UnsignedInt
                            ? Builder.BuildUIToFP(right.LLVMValue, destType.LLVMType)
                            : right.Type.Class is SignedInt
                                ? Builder.BuildSIToFP(right.LLVMValue, destType.LLVMType)
                                : throw new NotImplementedException()
                        : throw new NotImplementedException()
                : Builder.BuildCast(LLVMOpcode.LLVMBitCast,
                    right.LLVMValue,
                    destType.LLVMType);
        return new ValueContext(destType, builtVal);
    }

    public ValueContext CompilePow(ValueContext left, ValueContext right)
    {
        Class i16 = GetClass(Reserved.SignedInt16);
        Class i32 = GetClass(Reserved.SignedInt32);
        Class i64 = GetClass(Reserved.SignedInt64);
        Class f32 = GetClass(Reserved.Float32);
        Class f64 = GetClass(Reserved.Float64);

        LLVMValueRef val;
        string intrinsic;
        LLVMTypeRef destType = LLVMTypeRef.Float;
        bool returnInt = left.Type.Class is Int && right.Type.Class is Int;

        if (left.Type.Class is Int)
        {
            if (left.Type.Class.Name is Reserved.UnsignedInt64
                or Reserved.SignedInt64)
            {
                destType = LLVMTypeRef.Double;
            }

            val = left.Type.Class is SignedInt
                ? Builder.BuildSIToFP(SafeLoad(left).LLVMValue, destType)
                : left.Type.Class is UnsignedInt
                    ? Builder.BuildUIToFP(SafeLoad(left).LLVMValue, destType)
                    : throw new NotImplementedException();
            left = new ValueContext(val.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
                    ? f64.Type
                    : f32.Type,
                val);
        }
        else if (left.Type.Class is Float
            && left.Type.Class.Name != Reserved.Float64)
        {
            val = Builder.BuildFPCast(SafeLoad(left).LLVMValue, destType);
            left = new ValueContext(val.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
                    ? f64.Type
                    : f32.Type,
                val);
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
                    val = Builder.BuildFPCast(right.LLVMValue, LLVMTypeRef.Double);
                    right = new ValueContext(f64.Type, val);
                }

                intrinsic = "llvm.pow.f64";
            }
            else
            {
                if (right.Type.Class.Name != Reserved.Float32)
                {
                    val = Builder.BuildFPCast(right.LLVMValue, LLVMTypeRef.Float);
                    right = new ValueContext(f32.Type, val);
                }

                intrinsic = "llvm.pow.f32";
            }
        }
        else if (right.Type.Class is Int)
        {
            if (left.Type.Class.Name == Reserved.Float64)
            {
                if (right.Type.Class.Name is not Reserved.SignedInt16
                    and not Reserved.UnsignedInt16)
                {
                    val = Builder.BuildIntCast(right.LLVMValue, LLVMTypeRef.Int16);
                    right = new ValueContext(i16.Type, val);
                }

                intrinsic = "llvm.powi.f64.i16";
            }
            else
            {
                if (right.Type.Class.Name is not Reserved.SignedInt32
                    and not Reserved.UnsignedInt32)
                {
                    val = Builder.BuildIntCast(right.LLVMValue, LLVMTypeRef.Int32);
                    right = new ValueContext(i32.Type, val);
                }

                intrinsic = "llvm.powi.f32.i32";
            }
        }
        else
        {
            throw new NotImplementedException();
        }

        IntrinsicFunction func = GetIntrinsic(intrinsic);
        ReadOnlySpan<LLVMValueRef> parameters = stackalloc LLVMValueRef[]
        {
            SafeLoad(left).LLVMValue,
            SafeLoad(right).LLVMValue,
        };
        var result = new ValueContext(left.Type, func.Call(Builder, parameters));

        if (returnInt)
        {
            result = result.LLVMValue.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
                ? new ValueContext(i64.Type,
                    Builder.BuildFPToSI(result.LLVMValue,
                        LLVMTypeRef.Int64))
                : new ValueContext(i32.Type,
                    Builder.BuildFPToSI(result.LLVMValue,
                        LLVMTypeRef.Int32));
        }

        return result;
    }

    public ValueContext CompileAssignment(Scope scope, BinaryOperationNode binaryOp)
    {
        ValueContext variableAssigned = CompileExpression(scope, binaryOp.Left); //TODO: does not work with arrays

        if (variableAssigned.Type is not BasedType varType)
        {
            throw new Exception($"Cannot assign to \"{variableAssigned.LLVMValue.PrintToString()}\" as it is not a pointer.");
        }

        ValueContext value = SafeLoad(CompileExpression(scope, binaryOp.Right));

        if (!varType.BaseType.Equals(value.Type))
        {
            throw new Exception($"Tried to assign value of type \"{value.Type}\" to variable of type \"{varType.BaseType}\". "
                + $"Left: \"{binaryOp.Left.GetDebugString()}\". Right: \"{binaryOp.Right.GetDebugString()}\".");
        }

        Builder.BuildStore(value.LLVMValue, variableAssigned.LLVMValue);
        return new ValueContext(WrapAsRef(variableAssigned.Type), variableAssigned.LLVMValue);
    }

    public ValueContext CompileLocal(Scope scope, LocalDefNode localDef)
    {
        ValueContext value = null;
        Type type;

        if (localDef is InferredLocalDefNode inferredLocalDef)
        {
            value = CompileExpression(scope, inferredLocalDef.Value);
            type = value.Type;
        }
        else
        {
            type = ResolveTypeRef(localDef.TypeRef);
        }

        LLVMValueRef @var = Builder.BuildAlloca(type.LLVMType, localDef.Name);
        scope.Variables.Add(localDef.Name, new Variable(localDef.Name, @var, type));

        if (value != null)
        {
            Builder.BuildStore(SafeLoad(value).LLVMValue, @var);
        }

        return new ValueContext(WrapAsRef(type), @var);
    }

    public ValueContext CompileRef(Scope scope, RefNode refNode)
    {
        ValueContext context = null;

        while (refNode != null)
        {
            if (refNode is ThisNode)
            {
                if (CurrentFunction.OwnerClass == null)
                {
                    throw new Exception("Attempted self-instance reference in a global function.");
                }

                if (CurrentFunction.Name == Reserved.Init)
                {
                    Variable self = scope.GetVariable(Reserved.Self);
                    context = new ValueContext(WrapAsRef(self.Type),
                            self.LLVMVariable);
                }
                else
                {
                    context = new ValueContext(WrapAsRef(CurrentFunction.OwnerClass.Type),
                        CurrentFunction.LLVMFunc.FirstParam);
                }

                refNode = refNode.Child;
            }
            else if (refNode is TypeRefNode typeRef)
            {
                Class @class = GetClass(UnVoid(typeRef));
                refNode = refNode.Child;

                if (refNode is FuncCallNode funcCall)
                {
                    context = CompileFuncCall(context, scope, funcCall, @class);
                    refNode = refNode.Child;
                }
                else
                {
                    @class.GetStaticField(refNode.Name);
                    throw new NotImplementedException();
                }
            }
            else if (refNode is FuncCallNode funcCall)
            {
                context = CompileFuncCall(context, scope, funcCall);
                refNode = refNode.Child;
            }
            else if (refNode is IndexAccessNode indexAccess)
            {
                context = SafeLoad(CompileVarRef(context, scope, refNode));

                Type resultType = context.Type is PtrType ptrType
                    ? ptrType.BaseType
                    : throw new Exception($"Tried to use an index access on non-pointer \"{context.Type.LLVMType}\".");

                context = new ValueContext(new RefType(resultType),
                    Builder.BuildInBoundsGEP2(resultType.LLVMType,
                        context.LLVMValue,
                        new LLVMValueRef[1]
                        {
                            Builder.BuildIntCast(SafeLoad(CompileExpression(scope, indexAccess.Index)).LLVMValue,
                                LLVMTypeRef.Int64)
                        }));
                refNode = refNode.Child;
            }
            else
            {
                context = CompileVarRef(context, scope, refNode);
                refNode = refNode.Child;
            }
        }

        return context;
    }

    public ValueContext CompileVarRef(ValueContext context, Scope scope, RefNode refNode)
    {
        if (context != null)
        {
            Field field = context.Type.Class.GetField(refNode.Name);
            Type type = SafeLoad(context).Type;
            return new ValueContext(WrapAsRef(field.Type),
                Builder.BuildStructGEP2(type.LLVMType,
                    context.LLVMValue,
                    field.FieldIndex,
                    field.Name));
        }
        else
        {
            return scope.Variables.TryGetValue(refNode.Name, out Variable @var)
                ? new ValueContext(WrapAsRef(@var.Type),
                    @var.LLVMVariable)
                : GlobalConstants.TryGetValue(refNode.Name, out Constant @const)
                    ? new ValueContext(WrapAsRef(@const.Type),
                                    @const.LLVMValue)
                    : throw new Exception($"Variable \"{refNode.Name}\" does not exist.");
        }
    }

    public ValueContext CompileFuncCall(ValueContext context, Scope scope, FuncCallNode funcCall,
        Class? staticClass = null)
    {
        var argTypes = new List<Type>();
        var args = new List<LLVMValueRef>();
        bool contextWasNull = context == null;

        if (context == null)
        {
            if (CurrentFunction.OwnerClass != null)
            {
                context = new ValueContext(CurrentFunction.OwnerClass.Type,
                    CurrentFunction.LLVMFunc.FirstParam);
            }
        }

        foreach (ExpressionNode arg in funcCall.Arguments)
        {
            ValueContext val = CompileExpression(scope, arg);
            argTypes.Add(val.Type is RefType @ref ? @ref.BaseType : val.Type);
            args.Add(SafeLoad(val).LLVMValue);
        }

        var sig = new Signature(funcCall.Name, argTypes);

        if (context != null && context.Type.Class.Methods.TryGetValue(sig, out Function func))
        {
            if (context.Type.LLVMType == func.LLVMFuncType.ParamTypes[0])
            {
                var newArgs = new List<LLVMValueRef> { context.LLVMValue };
                newArgs.AddRange(args);
                args = newArgs;
            }
            else
            {
                throw new Exception("Attempted to call a method on a different class. !!THIS IS NOT A USER ERROR. REPORT ASAP!!");
            }
        }
        else if (contextWasNull && GlobalFunctions.TryGetValue(sig, out func))
        {
            // Keep empty
        }
        else if (staticClass != null && staticClass.StaticMethods.TryGetValue(sig, out func))
        {
            // Keep empty
        }
        else
        {
            throw new Exception($"Function \"{funcCall.Name}\" does not exist.");
        }

        return new ValueContext(func.ReturnType,
            func.Call(Builder, args.AsReadonlySpan()));
    }

    public void ResolveAttribute(Function func, AttributeNode attribute)
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
                    LLVMValueRef llvmFunc = func.LLVMFunc;
                    llvmFunc.FunctionCallConv = str switch
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

    public ValueContext SafeLoad(ValueContext value)
    {
        return value.Type is RefType @ref && value.Type.Class.Name != Reserved.Void
            ? new ValueContext(@ref.BaseType, Builder.BuildLoad2(@ref.BaseType.LLVMType, value.LLVMValue))
            : value;
    }

    public RefType WrapAsRef(Type type)
        => type is RefType @ref
            ? @ref
            : new RefType(type);

    private void InsertDefaultTypes()
    {
        Classes.Add(Reserved.Void, new Class(Reserved.Void, LLVMTypeRef.Void, PrivacyType.Public));
        Classes.Add(Reserved.Float16, Float.Float16);
        Classes.Add(Reserved.Float32, Float.Float32);
        Classes.Add(Reserved.Float64, Float.Float64);
        Classes.Add(Reserved.Bool, UnsignedInt.Bool);
        Classes.Add(Reserved.Char, UnsignedInt.Char);
        Classes.Add(Reserved.UnsignedInt8, UnsignedInt.UInt8);
        Classes.Add(Reserved.UnsignedInt16, UnsignedInt.UInt16);
        Classes.Add(Reserved.UnsignedInt32, UnsignedInt.UInt32);
        Classes.Add(Reserved.UnsignedInt64, UnsignedInt.UInt64);
        Classes.Add(Reserved.SignedInt8, SignedInt.Int8);
        Classes.Add(Reserved.SignedInt16, SignedInt.Int16);
        Classes.Add(Reserved.SignedInt32, SignedInt.Int32);
        Classes.Add(Reserved.SignedInt64, SignedInt.Int64);

        foreach (Class @class in Classes.Values)
        {
            @class.AddBuiltins(this);
        }
    }

    private IntrinsicFunction CreateIntrinsic(string name)
    {
        Pow func = name switch
        {
            "llvm.powi.f32.i32" => new Pow(name, Module, Float.Float32.Type, LLVMTypeRef.Float, LLVMTypeRef.Int32),
            "llvm.powi.f64.i16" => new Pow(name, Module, Float.Float64.Type, LLVMTypeRef.Double, LLVMTypeRef.Int16),
            "llvm.pow.f32" => new Pow(name, Module, Float.Float32.Type, LLVMTypeRef.Float, LLVMTypeRef.Float),
            "llvm.pow.f64" => new Pow(name, Module, Float.Float64.Type, LLVMTypeRef.Double, LLVMTypeRef.Double),
            _ => throw new NotImplementedException($"Intrinsic \"{name}\" is not implemented."),
        };

        _intrinsics.Add(name, func);
        return func;
    }
}
