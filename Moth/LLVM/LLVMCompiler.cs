using Moth.AST;
using Moth.AST.Node;
using Moth.LLVM.Data;

namespace Moth.LLVM;

public class LLVMCompiler
{
    public LLVMContextRef Context { get; }
    public LLVMBuilderRef Builder { get; }
    public LLVMModuleRef Module { get; }
    public Dictionary<string, Class> Classes { get; } = new Dictionary<string, Class>();
    public FuncDictionary GlobalFunctions { get; } = new FuncDictionary();
    public Dictionary<string, Constant> GlobalConstants { get; } = new Dictionary<string, Constant>();
    public Dictionary<string, GenericClassNode> GenericClassTemplates { get; } = new Dictionary<string, GenericClassNode>();
    public GenericDictionary GenericClasses { get; } = new GenericDictionary();
    public LLVMFunction? CurrentFunction { get; set; }
    public string ModuleName { get; }

    private Logger _logger { get; } = new Logger("moth/compiler");
    private readonly Dictionary<string, IntrinsicFunction> _intrinsics = new Dictionary<string, IntrinsicFunction>();

    public LLVMCompiler(string moduleName)
    {
        ModuleName = moduleName;
        Context = LLVMContextRef.Global;
        Builder = Context.CreateBuilder();
        Module = Context.CreateModuleWithName(ModuleName);

        InsertDefaultTypes();
    }

    public LLVMCompiler(string moduleName, IReadOnlyCollection<ScriptAST> scripts) : this(moduleName)
    {
        Compile(scripts);
    }

    public IntrinsicFunction GetIntrinsic(string name)
    {
        if (_intrinsics.TryGetValue(name, out var func))
        {
            return func;
        }
        else
        {
            return CreateIntrinsic(name);
        }
    }

    public Function GetFunction(Signature sig)
    {
        if (GlobalFunctions.TryGetValue(sig, out var func))
        {
            return func;
        }
        else
        {
            throw new Exception($"Function \"{sig}\" does not exist.");
        }
    }

    public Class GetClass(string name)
    {
        if (Classes.TryGetValue(name, out var @class))
        {
            return @class;
        }
        else
        {
            throw new Exception($"Class \"{name}\" does not exist.");
        }
    }

    public void Warn(string message)
    {
        Log($"Warning: {message}");
    }

    public void Log(string message)
    {
        _logger.WriteLine(message);
    }

    public LLVMCompiler Compile(IReadOnlyCollection<ScriptAST> scripts)
    {
        foreach (var script in scripts)
        {
            foreach (var constDefNode in script.GlobalConstants)
            {
                DefineConstant(constDefNode);
            }

            foreach (var funcDefNode in script.GlobalFunctions)
            {
                DefineFunction(funcDefNode);
            }

            foreach (var @class in script.ClassNodes)
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

            foreach (var classNode in script.ClassNodes)
            {
                if (classNode is not GenericClassNode)
                {
                    var @class = GetClass(classNode.Name);

                    foreach (var funcDefNode in classNode.Scope.Statements.OfType<FuncDefNode>())
                    {
                        DefineFunction(funcDefNode, @class);
                    }
                }
            }
        }

        foreach (var script in scripts)
        {
            foreach (var @class in script.ClassNodes)
            {
                if (@class is not GenericClassNode)
                {
                    CompileClass(@class);
                }
            }
        }

        foreach (var script in scripts)
        {
            foreach (var funcDefNode in script.GlobalFunctions)
            {
                CompileFunction(funcDefNode);
            }

            foreach (var classNode in script.ClassNodes)
            {
                if (classNode is not GenericClassNode)
                {
                    var @class = GetClass(classNode.Name);

                    foreach (var funcDefNode in classNode.Scope.Statements.OfType<FuncDefNode>())
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
        Class newClass = new Class(classNode.Name, newStruct, classNode.Privacy);
        Classes.Add(classNode.Name, newClass);
        newClass.AddBuiltins(this);
    }

    public void CompileClass(ClassNode classNode)
    {
        var llvmTypes = new List<LLVMTypeRef>();
        var @class = GetClass(classNode.Name);
        uint index = 0;

        foreach (FieldDefNode field in classNode.Scope.Statements.OfType<FieldDefNode>())
        {
            var fieldType = ResolveTypeRef(field.TypeRef);
            llvmTypes.Add(fieldType.LLVMType);
            @class.Fields.Add(field.Name, new Field(field.Name, index, fieldType, field.Privacy));
            index++;
        }

        @class.Type.LLVMType.StructSetBody(llvmTypes.AsReadonlySpan(), false);
    }

    public void DefineFunction(FuncDefNode funcDefNode, Class @class = null)
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

        Signature sig = new Signature(funcDefNode.Name, paramTypes, funcDefNode.IsVariadic);
        string funcName = funcDefNode.Name == Reserved.Main || funcDefNode.Privacy == PrivacyType.Foreign
            ? funcDefNode.Name
            : sig.ToString();

        if (@class != null)
        {
            funcName = $"{@class.Name}.{funcName}";
        }

        Type returnType = ResolveTypeRef(funcDefNode.ReturnTypeRef);
        LLVMTypeRef llvmFuncType = LLVMTypeRef.CreateFunction(returnType.LLVMType, paramLLVMTypes.AsReadonlySpan(), funcDefNode.IsVariadic);
        LLVMValueRef llvmFunc = Module.AddFunction(funcName, llvmFuncType);
        LLVMFunction func = new LLVMFunction(funcDefNode.Name,
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

        foreach (var attribute in funcDefNode.Attributes)
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

    public void CompileFunction(FuncDefNode funcDefNode, Class @class = null)
    {
        Function fn;
        List<Type> paramTypes = new List<Type>();

        foreach (var param in funcDefNode.Params)
        {
            paramTypes.Add(ResolveParameter(param));
        }

        Signature sig = new Signature(funcDefNode.Name, paramTypes);

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
                var llvmField = Builder.BuildStructGEP2(@new.Type.LLVMType, @new.LLVMVariable, field.FieldIndex);
                var zeroedVal = LLVMValueRef.CreateConstNull(field.Type.LLVMType);

                Builder.BuildStore(zeroedVal, llvmField);
            }
        }

        foreach (Parameter param in CurrentFunction.Params)
        {
            var paramAsVar = Builder.BuildAlloca(param.Type.LLVMType, param.Name);
            Builder.BuildStore(func.LLVMFunc.Params[param.ParamIndex], paramAsVar);
            func.OpeningScope.Variables.Add(param.Name,
                new Variable(param.Name,
                    paramAsVar,
                    param.Type));
        }

        if (!CompileScope(func.OpeningScope, funcDefNode.ExecutionBlock))
        {
            throw new Exception("Function is not guaranteed to return.");
        }
    }

    public Type ResolveParameter(ParameterNode param)
    {
        var type = ResolveTypeRef(param.TypeRef);

        if (param.RequireRefType)
        {
            var newType = new RefType(type, LLVMTypeRef.CreatePointer(type.LLVMType, 0), type.Class);
            return new PtrType(newType, LLVMTypeRef.CreatePointer(newType.LLVMType, 0), newType.Class);
        }
        else
        {
            return type;
        }
    }

    public void DefineConstant(FieldDefNode constDef, Class @class = null)
    {
        Type constType = ResolveTypeRef(constDef.TypeRef);
        var constVal = Module.AddGlobal(constType.LLVMType, constDef.Name);

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
                    var expr = SafeLoad(CompileExpression(scope, @return.ReturnValue));

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
                var newScope = new Scope(CurrentFunction.LLVMFunc.AppendBasicBlock(""));
                newScope.Variables = new Dictionary<string, Variable>(scope.Variables);
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
                var loop = CurrentFunction.LLVMFunc.AppendBasicBlock("loop");
                var then = CurrentFunction.LLVMFunc.AppendBasicBlock("then");
                var @continue = CurrentFunction.LLVMFunc.AppendBasicBlock("continue");
                Builder.BuildBr(loop);
                Builder.PositionAtEnd(loop);

                var condition = CompileExpression(scope, @while.Condition);
                Builder.BuildCondBr(SafeLoad(condition).LLVMValue, then, @continue);
                Builder.PositionAtEnd(then);

                var newScope = new Scope(then);
                newScope.Variables = new Dictionary<string, Variable>(scope.Variables);

                if (!CompileScope(newScope, @while.Then))
                {
                    Builder.BuildBr(@loop);
                }

                Builder.PositionAtEnd(@continue);
                scope.LLVMBlock = @continue;
            }
            else if (statement is IfNode @if)
            {
                var condition = CompileExpression(scope, @if.Condition);
                var then = CurrentFunction.LLVMFunc.AppendBasicBlock("then");
                var @else = CurrentFunction.LLVMFunc.AppendBasicBlock("else");
                LLVMBasicBlockRef @continue = null;
                bool thenReturned = false;
                bool elseReturned = false;

                Builder.BuildCondBr(SafeLoad(condition).LLVMValue, then, @else);

                //then
                {
                    Builder.PositionAtEnd(then);

                    {
                        var newScope = new Scope(then);
                        newScope.Variables = new Dictionary<string, Variable>(scope.Variables);

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

                    var newScope = new Scope(@else);
                    newScope.Variables = new Dictionary<string, Variable>(scope.Variables);

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

        if (typeRef is GenericTypeRefNode genTypeRef)
        {
            throw new NotImplementedException();
        }
        else
        {
            var @class = GetClass(UnVoid(typeRef));
            type = new Type(@class.Type.LLVMType, @class, TypeKind.Class);
        }

        int index = 0;

        while (index < typeRef.PointerDepth)
        {
            type = new PtrType(type, LLVMTypeRef.CreatePointer(type.LLVMType, 0), type.Class);
            index++;
        }

        return type;
    }

    public ValueContext CompileExpression(Scope scope, ExpressionNode expr)
    {
        if (expr is BinaryOperationNode binaryOp)
        {
            if (binaryOp.Type == OperationType.Assignment)
            {
                return CompileAssignment(scope, binaryOp);
            }
            else if (binaryOp.Type == OperationType.Cast)
            {
                return CompileCast(scope, binaryOp);
            }
            else
            {
                return CompileOperation(scope, binaryOp);
            }
        }
        else if (expr is LocalDefNode localDef)
        {
            return CompileLocal(scope, localDef);
        }
        else if (expr is InlineIfNode @if)
        {
            var condition = CompileExpression(scope, @if.Condition);
            var then = CurrentFunction.LLVMFunc.AppendBasicBlock("then");
            var @else = CurrentFunction.LLVMFunc.AppendBasicBlock("else");
            var @continue = CurrentFunction.LLVMFunc.AppendBasicBlock("continue");

            //then
            Builder.PositionAtEnd(then);
            var thenVal = CompileExpression(scope, @if.Then);

            //else
            Builder.PositionAtEnd(@else);
            var elseVal = CompileExpression(scope, @if.Else);

            //prior
            Builder.PositionAtEnd(scope.LLVMBlock);
            var result = Builder.BuildAlloca(thenVal.Type.LLVMType, "result");
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

            if (thenVal.Type.Class.Name != elseVal.Type.Class.Name)
            {
                throw new Exception("Then and else statements of inline if are not of the same type.");
            }

            return new ValueContext(WrapAsRef(thenVal.Type), result);
        }
        else if (expr is ConstantNode constNode)
        {
            return CompileLiteral(scope, constNode);
        }
        else if (expr is InverseNode inverse)
        {
            var @ref = CompileRef(scope, inverse.Value);
            return new ValueContext(@ref.Type,
                Builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ,
                    SafeLoad(@ref).LLVMValue,
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0)));
        }
        else if (expr is IncrementVarNode incrementVar)
        {
            var @ref = CompileRef(scope, incrementVar.RefNode);
            var valToAdd = LLVMValueRef.CreateConstInt(@ref.Type.Class.Type.LLVMType, 1); //TODO: float compat?
            Builder.BuildStore(Builder.BuildAdd(SafeLoad(@ref).LLVMValue, valToAdd), @ref.LLVMValue);
            return new ValueContext(WrapAsRef(@ref.Type),
                @ref.LLVMValue);
        }
        else if (expr is DecrementVarNode decrementVar)
        {
            var @ref = CompileRef(scope, decrementVar.RefNode);
            var valToSub = LLVMValueRef.CreateConstInt(@ref.Type.Class.Type.LLVMType, 1); //TODO: float compat?
            Builder.BuildStore(Builder.BuildSub(SafeLoad(@ref).LLVMValue, valToSub), @ref.LLVMValue);
            return new ValueContext(WrapAsRef(@ref.Type),
                @ref.LLVMValue);
        }
        else if (expr is AsReferenceNode asReference)
        {
            var value = CompileExpression(scope, asReference.Value);
            var newVal = Builder.BuildAlloca(value.Type.LLVMType);
            Builder.BuildStore(value.LLVMValue, newVal);
            return new ValueContext(new PtrType(value.Type, LLVMTypeRef.CreatePointer(value.Type.LLVMType, 0), value.Type.Class), newVal);
        }
        else if (expr is DeReferenceNode deReference)
        {
            var value = SafeLoad(CompileExpression(scope, deReference.Value));

            if (value.Type is PtrType ptrType)
            {
                return new ValueContext(ptrType.BaseType, Builder.BuildLoad2(value.Type.LLVMType, value.LLVMValue));
            }
            else
            {
                throw new Exception("Attempted to load a non-pointer.");
            }
        }
        else if (expr is RefNode @ref)
        {
            return CompileRef(scope, @ref);
        }
        else if (expr is SubExprNode subExpr)
        {
            return CompileExpression(scope, subExpr.Expression);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public ValueContext CompileLiteral(Scope scope, ConstantNode constNode)
    {
        if (constNode.Value is string str)
        {
            var @class = GetClass(Reserved.Char);
            var constStr = Context.GetConstString(str, false);
            var global = Module.AddGlobal(constStr.TypeOf, "");
            global.Initializer = constStr;
            return new ValueContext(new PtrType(@class.Type, LLVMTypeRef.CreatePointer(@class.Type.LLVMType, 0), @class), global);
        }
        else if (constNode.Value is bool @bool)
        {
            var @class = GetClass(Reserved.Bool);
            return new ValueContext(@class.Type, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, (ulong)(@bool ? 1 : 0))); //TODO: why -1
        }
        else if (constNode.Value is int i32)
        {
            var @class = GetClass(Reserved.SignedInt32);
            return new ValueContext(@class.Type, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)i32, true));
        }
        else if (constNode.Value is float f32)
        {
            var @class = GetClass(Reserved.Float32);
            return new ValueContext(@class.Type, LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, f32));
        }
        else if (constNode.Value is char ch)
        {
            var @class = GetClass(Reserved.Char);
            return new ValueContext(@class.Type, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, (ulong)ch));
        }
        else if (constNode.Value == null)
        {
            var @class = GetClass(Reserved.Char);
            return new ValueContext(@class.Type, LLVMValueRef.CreateConstNull(@class.Type.LLVMType));
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public ValueContext CompileOperation(Scope scope, BinaryOperationNode binaryOp)
    {
        var left = SafeLoad(CompileExpression(scope, binaryOp.Left));
        var right = SafeLoad(CompileExpression(scope, binaryOp.Right));

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
                    if (left.Type.Class is Float)
                    {
                        builtVal = Builder.BuildFAdd(leftVal, rightVal);
                    }
                    else
                    {
                        builtVal = Builder.BuildAdd(leftVal, rightVal);
                    }

                    break;
                case OperationType.Subtraction:
                    if (left.Type.Class is Float)
                    {
                        builtVal = Builder.BuildFSub(leftVal, rightVal);
                    }
                    else
                    {
                        builtVal = Builder.BuildSub(leftVal, rightVal);
                    }

                    break;
                case OperationType.Multiplication:
                    if (left.Type.Class is Float)
                    {
                        builtVal = Builder.BuildFMul(leftVal, rightVal);
                    }
                    else
                    {
                        builtVal = Builder.BuildMul(leftVal, rightVal);
                    }

                    break;
                case OperationType.Division:
                    if (left.Type.Class is Float)
                    {
                        builtVal = Builder.BuildFDiv(leftVal, rightVal);
                    }
                    else if (left.Type.Class is UnsignedInt)
                    {
                        builtVal = Builder.BuildUDiv(leftVal, rightVal);
                    }
                    else if (left.Type.Class is SignedInt)
                    {
                        builtVal = Builder.BuildSDiv(leftVal, rightVal);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    break;
                case OperationType.Modulo:
                    if (left.Type.Class is Float)
                    {
                        builtVal = Builder.BuildFRem(leftVal, rightVal);
                    }
                    else if (left.Type.Class is UnsignedInt)
                    {
                        builtVal = Builder.BuildURem(leftVal, rightVal);
                    }
                    else if (left.Type.Class is SignedInt)
                    {
                        builtVal = Builder.BuildSRem(leftVal, rightVal);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

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
                    if (right.LLVMValue.IsNull)
                    {
                        builtVal = binaryOp.Type == OperationType.Equal
                            ? Builder.BuildIsNull(leftVal)
                            : Builder.BuildIsNotNull(leftVal);
                    }
                    else if (left.LLVMValue.IsNull)
                    {
                        builtVal = binaryOp.Type == OperationType.Equal
                            ? Builder.BuildIsNull(rightVal)
                            : Builder.BuildIsNotNull(rightVal);
                    }
                    else if (left.Type.Class is Float)
                    {
                        builtVal = Builder.BuildFCmp(binaryOp.Type == OperationType.Equal
                            ? LLVMRealPredicate.LLVMRealOEQ
                            : LLVMRealPredicate.LLVMRealUNE,
                            leftVal, rightVal);
                    }
                    else if (left.Type.Class is Int)
                    {
                        builtVal = Builder.BuildICmp(binaryOp.Type == OperationType.Equal
                            ? LLVMIntPredicate.LLVMIntEQ
                            : LLVMIntPredicate.LLVMIntNE,
                            leftVal, rightVal);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    builtType = UnsignedInt.Bool.Type;
                    break;
                case OperationType.GreaterThan:
                case OperationType.GreaterThanOrEqual:
                case OperationType.LesserThan:
                case OperationType.LesserThanOrEqual:
                    if (left.Type.Class is Float)
                    {
                        builtVal = Builder.BuildFCmp(binaryOp.Type switch
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
                        builtVal = Builder.BuildICmp(binaryOp.Type switch
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
                        builtVal = Builder.BuildICmp(binaryOp.Type switch
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
                        throw new NotImplementedException($"Unimplemented comparison between {left.Type.Class.Name} and {right.Type.Class.Name}.");
                    }

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

        var right = SafeLoad(CompileExpression(scope, binaryOp.Right));
        Type destType = ResolveTypeRef(left);
        LLVMValueRef builtVal;

        if (destType.Class is Int) //TODO: doesn't work for pointer casts
        {
            if (right.Type.Class is Int)
            {
                if (destType.Class.Name == Reserved.Bool)
                {
                    builtVal = Builder.BuildICmp(LLVMIntPredicate.LLVMIntNE,
                        LLVMValueRef.CreateConstInt(right.Type.LLVMType, 0), right.LLVMValue);
                }
                else if (right.Type.Class.Name == Reserved.Bool)
                {
                    builtVal = Builder.BuildZExt(right.LLVMValue, destType.LLVMType);
                }
                else
                {
                    builtVal = Builder.BuildIntCast(right.LLVMValue, destType.LLVMType);
                }
            }
            else if (right.Type.Class is Float)
            {
                if (destType.Class is UnsignedInt)
                {
                    builtVal = Builder.BuildFPToUI(right.LLVMValue, destType.LLVMType);
                }
                else if (destType.Class is SignedInt)
                {
                    builtVal = Builder.BuildFPToSI(right.LLVMValue, destType.LLVMType);
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
        else if (destType.Class is Float)
        {
            if (right.Type.Class is Float)
            {
                builtVal = Builder.BuildFPCast(right.LLVMValue, destType.LLVMType);
            }
            else if (right.Type.Class is Int)
            {
                if (right.Type.Class is UnsignedInt)
                {
                    builtVal = Builder.BuildUIToFP(right.LLVMValue, destType.LLVMType);
                }
                else if (right.Type.Class is SignedInt)
                {
                    builtVal = Builder.BuildSIToFP(right.LLVMValue, destType.LLVMType);
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
            builtVal = Builder.BuildCast(LLVMOpcode.LLVMBitCast,
                right.LLVMValue,
                destType.LLVMType);
        }

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
        var destType = LLVMTypeRef.Float;
        bool returnInt = left.Type.Class is Int && right.Type.Class is Int;

        if (left.Type.Class is Int)
        {
            if (left.Type.Class.Name == Reserved.UnsignedInt64
                || left.Type.Class.Name == Reserved.SignedInt64)
            {
                destType = LLVMTypeRef.Double;
            }

            if (left.Type.Class is SignedInt)
            {
                val = Builder.BuildSIToFP(SafeLoad(left).LLVMValue, destType);
            }
            else if (left.Type.Class is UnsignedInt)
            {
                val = Builder.BuildUIToFP(SafeLoad(left).LLVMValue, destType);
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
            val = Builder.BuildFPCast(SafeLoad(left).LLVMValue, destType);
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
                if (right.Type.Class.Name != Reserved.SignedInt16
                    && right.Type.Class.Name != Reserved.UnsignedInt16)
                {
                    val = Builder.BuildIntCast(right.LLVMValue, LLVMTypeRef.Int16);
                    right = new ValueContext(i16.Type, val);
                }

                intrinsic = "llvm.powi.f64.i16";
            }
            else
            {
                if (right.Type.Class.Name != Reserved.SignedInt32
                    && right.Type.Class.Name != Reserved.UnsignedInt32)
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

        var func = GetIntrinsic(intrinsic);
        ReadOnlySpan<LLVMValueRef> parameters = stackalloc LLVMValueRef[]
        {
            SafeLoad(left).LLVMValue,
            SafeLoad(right).LLVMValue,
        };
        var result = new ValueContext(left.Type, func.Call(Builder, parameters));

        if (returnInt)
        {
            if (result.LLVMValue.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind)
            {
                result = new ValueContext(i64.Type,
                    Builder.BuildFPToSI(result.LLVMValue,
                        LLVMTypeRef.Int64));
            }
            else
            {
                result = new ValueContext(i32.Type,
                    Builder.BuildFPToSI(result.LLVMValue,
                        LLVMTypeRef.Int32));
            }
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

        var value = SafeLoad(CompileExpression(scope, binaryOp.Right));

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

        var @var = Builder.BuildAlloca(type.LLVMType, localDef.Name);
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
                    var self = scope.GetVariable(Reserved.Self);
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
                var @class = GetClass(UnVoid(typeRef));
                refNode = refNode.Child;

                if (refNode is FuncCallNode funcCall)
                {
                    context = CompileFuncCall(context, scope, funcCall, @class);
                    refNode = refNode.Child;
                }
                else
                {
                    var field = @class.GetStaticField(refNode.Name);
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

                var resultType = context.Type is PtrType ptrType
                    ? ptrType.BaseType
                    : throw new Exception($"Tried to use an index access on non-pointer \"{context.Type.LLVMType}\".");

                context = new ValueContext(new RefType(resultType,
                        LLVMTypeRef.CreatePointer(resultType.LLVMType, 0),
                        resultType.Class),
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
            var field = context.Type.Class.GetField(refNode.Name);
            var type = SafeLoad(context).Type;
            return new ValueContext(WrapAsRef(field.Type),
                Builder.BuildStructGEP2(type.LLVMType,
                    context.LLVMValue,
                    field.FieldIndex,
                    field.Name));
        }
        else
        {
            if (scope.Variables.TryGetValue(refNode.Name, out Variable @var))
            {
                return new ValueContext(WrapAsRef(@var.Type),
                    @var.LLVMVariable);
            }
            else if (GlobalConstants.TryGetValue(refNode.Name, out Constant @const))
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

    public ValueContext CompileFuncCall(ValueContext context, Scope scope, FuncCallNode funcCall,
        Class staticClass = null)
    {
        List<Type> argTypes = new List<Type>();
        List<LLVMValueRef> args = new List<LLVMValueRef>();
        bool contextWasNull = context == null;
        Function func;

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
            var val = CompileExpression(scope, arg);
            argTypes.Add(val.Type is RefType @ref ? @ref.BaseType : val.Type);
            args.Add(SafeLoad(val).LLVMValue);
        }

        Signature sig = new Signature(funcCall.Name, argTypes);

        if (context != null && context.Type.Class.Methods.TryGetValue(sig, out func))
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
                    var llvmFunc = func.LLVMFunc;
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
        if (value.Type is RefType @ref && value.Type.Class.Name != Reserved.Void)
        {
            return new ValueContext(@ref.BaseType, Builder.BuildLoad2(@ref.BaseType.LLVMType, value.LLVMValue));
        }
        else
        {
            return value;
        }
    }

    public RefType WrapAsRef(Type type)
    {
        if (type is RefType @ref)
        {
            return @ref;
        }

        return new RefType(type, LLVMTypeRef.CreatePointer(type.LLVMType, 0), type.Class);
    }
    
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
        var func = name switch
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
