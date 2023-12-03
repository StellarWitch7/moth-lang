using Moth.AST;
using Moth.AST.Node;
using Moth.LLVM.Data;

namespace Moth.LLVM;

public class LLVMCompiler
{
    public string ModuleName { get; }
    public bool DoOptimize { get; }
    public LLVMContextRef Context { get; }
    public LLVMModuleRef Module { get; }
    public LLVMBuilderRef Builder { get; }
    public LLVMPassManagerRef FunctionPassManager { get; }
    public Namespace GlobalNamespace { get; }

    private readonly Logger _logger = new Logger("moth");
    private readonly Dictionary<string, IntrinsicFunction> _intrinsics = new Dictionary<string, IntrinsicFunction>();
    private Namespace[] _imports = null;
    private Namespace? _currentNamespace;
    private Function? _currentFunction;

    public LLVMCompiler(string moduleName, bool doOptimize = true)
    {
        ModuleName = moduleName;
        DoOptimize = doOptimize;
        Context = LLVMContextRef.Global;
        Builder = Context.CreateBuilder();
        Module = Context.CreateModuleWithName(ModuleName);
        GlobalNamespace = InitGlobalNamespace();

        Log("(unsafe) Creating function optimization pass manager...");
        
        unsafe
        {
            FunctionPassManager = LLVMSharp.Interop.LLVM.CreateFunctionPassManagerForModule(Module);
            
            LLVMSharp.Interop.LLVM.AddInstructionCombiningPass(FunctionPassManager);
            LLVMSharp.Interop.LLVM.AddReassociatePass(FunctionPassManager);
            LLVMSharp.Interop.LLVM.AddGVNPass(FunctionPassManager);
            LLVMSharp.Interop.LLVM.AddCFGSimplificationPass(FunctionPassManager);

            LLVMSharp.Interop.LLVM.InitializeFunctionPassManager(FunctionPassManager);
        }
    }

    public LLVMCompiler(string moduleName, IReadOnlyCollection<ScriptAST> scripts) : this(moduleName) => Compile(scripts);

    public IContainer Parent
    {
        get
        {
            return CurrentNamespace;
        }
    }
    
    private Namespace CurrentNamespace
    {
        get
        {
            return _currentNamespace ?? throw new Exception("Current namespace is null.");
        }

        set
        {
            _currentNamespace = value;
        }
    }

    private Function CurrentFunction
    {
        get
        {
            return _currentFunction ?? throw new Exception("Current function is null. " +
                "This is a CRITICAL ERROR. Report ASAP.");
        }
        
        set
        {
            if (value == null)
            {
                _currentFunction = null;
            }
            else
            {
                _currentFunction = value.Type is LLVMFuncType
                    ? value
                    : throw new Exception("Cannot assign function value as it is not of a valid type. " +
                        "This is a CRITICAL ERROR. Report ASAP.");
            }
        }
    }

    public IntrinsicFunction GetIntrinsic(string name)
        => _intrinsics.TryGetValue(name, out IntrinsicFunction? func)
            ? func
            : CreateIntrinsic(name);

    public Namespace ResolveNamespace(string str) //TODO: improve
    {
        Namespace value = null;
        string[] names = str.Split('.');
        uint index = 0;
        
        foreach (var name in names) //TODO: is this unnecessary?
        {
            names[index] = name.Replace(".", "");
            index++;
        }
        foreach (var name in names)
        {
            if (value != null)
            {
                if (value.Namespaces.TryGetValue(name, out Namespace nmspace))
                {
                    value = nmspace;
                }
                else
                {
                    var @new = new Namespace(value, name);
                    value.Namespaces.Add(name, @new);
                    value = @new;
                }
            }
            else
            {
                if (GlobalNamespace.Namespaces.TryGetValue(name, out Namespace o))
                {
                    value = o;
                }
                else
                {
                    var @new = new Namespace(GlobalNamespace, name);
                    GlobalNamespace.Namespaces.Add(name, @new);
                    value = @new;
                }
            }
        }

        return value;
    }

    public Namespace[] ResolveImports(string[] importNames)
    {
        List<Namespace> result = new List<Namespace>();

        foreach (var importName in importNames)
        {
            result.Add(ResolveNamespace(importName));
        }

        return result.ToArray();
    }

    public void Warn(string message) => Log($"Warning: {message}");

    public void Log(string message) => _logger.WriteLine(message);

    public LLVMCompiler Compile(IReadOnlyCollection<ScriptAST> scripts)
    {
        foreach (ScriptAST script in scripts)
        {
            OpenFile(script.Namespace, script.Imports.ToArray());

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
                    CurrentNamespace.GenericClassTemplates.Add(genericClass.Name, genericClass);
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
                    Struct @struct = GetStruct(classNode.Name);

                    foreach (FuncDefNode funcDefNode in classNode.Scope.Statements.OfType<FuncDefNode>())
                    {
                        DefineFunction(funcDefNode, @struct);
                    }
                }
            }
        }

        foreach (ScriptAST script in scripts)
        {
            OpenFile(script.Namespace, script.Imports.ToArray());

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
            OpenFile(script.Namespace, script.Imports.ToArray());

            foreach (FuncDefNode funcDefNode in script.GlobalFunctions)
            {
                CompileFunction(funcDefNode);
            }

            foreach (ClassNode classNode in script.ClassNodes)
            {
                if (classNode is not GenericClassNode)
                {
                    Struct @struct = GetStruct(classNode.Name);

                    foreach (FuncDefNode funcDefNode in classNode.Scope.Statements.OfType<FuncDefNode>())
                    {
                        CompileFunction(funcDefNode, @struct);
                    }
                }
            }
        }

        return this;
    }

    public void DefineClass(ClassNode classNode)
    {
        Struct newStruct;
        
        if (classNode.IsStruct)
        {
            if (classNode.Scope == null)
            {
                newStruct = new OpaqueStruct(this,
                    classNode.Name,
                    classNode.Privacy);
            }
            else
            {
                newStruct = new Struct(CurrentNamespace,
                    classNode.Name,
                    Context.CreateNamedStruct(classNode.Name),
                    classNode.Privacy);
            }
        }
        else
        {
            if (classNode.Scope == null)
            {
                throw new Exception($"Class \"{classNode.Name}\" cannot be foreign.");
            }
            
            newStruct = new Class(CurrentNamespace,
                classNode.Name,
                Context.CreateNamedStruct(classNode.Name),
                classNode.Privacy);
        }
        
        CurrentNamespace.Structs.Add(classNode.Name, newStruct);
        newStruct.AddBuiltins(this);
    }

    public void CompileClass(ClassNode classNode)
    {
        var llvmTypes = new List<LLVMTypeRef>();
        Struct @struct = GetStruct(classNode.Name);
        uint index = 0;

        foreach (FieldDefNode field in classNode.Scope.Statements.OfType<FieldDefNode>())
        {
            Type fieldType = ResolveType(field.TypeRef);
            llvmTypes.Add(fieldType.LLVMType);
            @struct.Fields.Add(field.Name, new Field(field.Name, index, fieldType, field.Privacy));
            index++;
        }

        @struct.LLVMType.StructSetBody(llvmTypes.AsReadonlySpan(), false);
    }

    public void DefineFunction(FuncDefNode funcDefNode, Struct? @struct = null)
    {
        uint index = 0;
        var @params = new List<Parameter>();
        var paramTypes = new List<Type>();

        if (@struct != null && !funcDefNode.IsStatic)
        {
            paramTypes.Add(new PtrType(@struct));
            index++;
        }

        foreach (ParameterNode paramNode in funcDefNode.Params)
        {
            Type paramType = ResolveParameter(paramNode);
            paramNode.TypeRef.Name = UnVoid(paramNode.TypeRef);
            @params.Add(new Parameter(index, paramNode.Name));
            paramTypes.Add(paramType);
            index++;
        }

        var sig = new Signature(funcDefNode.Name, paramTypes, funcDefNode.IsVariadic);
        string funcName = funcDefNode.Name == Reserved.Main || funcDefNode.IsForeign
            ? funcDefNode.Name
            : sig.ToString();

        if (@struct != null)
        {
            funcName = $"{@struct.Name}.{funcName}";
        }

        Type returnType = ResolveType(funcDefNode.ReturnTypeRef);
        LLVMTypeRef llvmFuncType = LLVMTypeRef.CreateFunction(returnType.LLVMType,
            paramTypes.AsLLVMTypes().ToArray(),
            funcDefNode.IsVariadic);
        LLVMFuncType funcType = @struct == null
            ? new LLVMFuncType(funcName, returnType, paramTypes.ToArray(), funcDefNode.IsVariadic)
            : new MethodType(funcName, returnType, paramTypes.ToArray(), funcDefNode.IsVariadic, @struct);
        DefinedFunction func = new DefinedFunction(@struct == null
                ? CurrentNamespace
                : @struct,
            funcType,
            Module.AddFunction(funcName, llvmFuncType),
            @params.ToArray(),
            funcDefNode.Privacy);

        if (@struct != null)
        {
            if (funcDefNode.IsStatic)
            {
                @struct.StaticMethods.Add(sig, func);
            }
            else if (@struct is Class @class)
            {
                @class.Methods.Add(sig, func);
            }
            else
            {
                throw new Exception($"Cannot have instance method \"{func.Type.Name}\" on struct \"{@struct.Name}\"");
            }
        }
        else
        {
            CurrentNamespace.Functions.Add(sig, func);
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

    public void CompileFunction(FuncDefNode funcDefNode, Struct? @struct = null)
    {
        Function? func;
        var paramTypes = new List<Type>();

        foreach (ParameterNode param in funcDefNode.Params)
        {
            paramTypes.Add(ResolveParameter(param));
        }

        var sig = new Signature(funcDefNode.Name, paramTypes);

        if (funcDefNode.IsForeign && funcDefNode.ExecutionBlock == null)
        {
            return;
        }
        else if (@struct != null && @struct is Class @class && !funcDefNode.IsStatic && @class.Methods.TryGetValue(sig, out func))
        {
            // Keep empty
        }
        else if (@struct != null && funcDefNode.IsStatic && @struct.StaticMethods.TryGetValue(sig, out func))
        {
            // Keep empty
        }
        else if (CurrentNamespace.Functions.TryGetValue(sig, out func))
        {
            // Keep empty
        }
        else
        {
            throw new Exception($"Cannot compile function {funcDefNode.Name} as it is undefined.");
        }
        
        CurrentFunction = func;
        func.OpeningScope = new Scope(func.LLVMValue.AppendBasicBlock("entry"));
        Builder.PositionAtEnd(func.OpeningScope.LLVMBlock);

        LLVMFuncType funcType = (LLVMFuncType)func.Type;

        if (funcType is MethodType methodType && funcDefNode.Name == Reserved.Init && funcDefNode.IsStatic)
        {
            if (funcType.ReturnType is Class retType)
            {
                if (retType != methodType.OwnerStruct)
                {
                    throw new Exception($"Init method does not return the same type as its owner class " +
                        $"(\"{methodType.OwnerStruct.Name}\").");
                }
            }
            else
            {
                //TODO: what is this
            }

            var @new = new Variable(Reserved.Self,
                new RefType(methodType.OwnerStruct),
                Builder.BuildMalloc(methodType.OwnerStruct.LLVMType, Reserved.Self));

            func.OpeningScope.Variables.Add(@new.Name, @new);

            if (@new.Type is not Class classOfNew)
            {
                throw new Exception();
            }
            
            foreach (Field field in classOfNew.Fields.Values)
            {
                LLVMValueRef llvmField = Builder.BuildStructGEP2(@new.Type.LLVMType, @new.LLVMValue, field.FieldIndex);
                var zeroedVal = LLVMValueRef.CreateConstNull(field.Type.LLVMType);

                Builder.BuildStore(zeroedVal, llvmField);
            }
        }

        foreach (Parameter param in func.Params)
        {
            LLVMValueRef paramAsVar = Builder.BuildAlloca(func.Type.ParameterTypes[param.ParamIndex].LLVMType,
                param.Name);
            Builder.BuildStore(func.LLVMValue.Params[param.ParamIndex], paramAsVar);
            func.OpeningScope.Variables.Add(param.Name,
                new Variable(param.Name,
                    new RefType(CurrentFunction.Type.ParameterTypes[param.ParamIndex]),
                    paramAsVar));
        }
        
        if (CompileScope(func.OpeningScope, funcDefNode.ExecutionBlock))
        {
            if (DoOptimize)
            {
                Log($"(unsafe) Running optimization pass on function \"{funcDefNode.Name}\".");
            
                unsafe
                {
                    LLVMSharp.Interop.LLVM.RunFunctionPassManager(FunctionPassManager, func.LLVMValue);
                }
            }
        }
        else
        {
            throw new Exception("Function is not guaranteed to return.");
        }
    }

    public Type ResolveParameter(ParameterNode param)
    {
        Type type = ResolveType(param.TypeRef);

        return param.RequireRefType
            ? new PtrType(new RefType(type))
            : type;
    }

    public void DefineConstant(FieldDefNode constDef, Class? @class = null)
    {
        Type constType = ResolveType(constDef.TypeRef);
        LLVMValueRef constVal = Module.AddGlobal(constType.LLVMType, constDef.Name);

        if (@class != null)
        {
            @class.Constants.Add(constDef.Name, new Constant(constType, constVal));
        }
        else
        {
            CurrentNamespace.Constants.Add(constDef.Name, new Constant(constType, constVal));
        }
    }

    public bool CompileScope(Scope scope, ScopeNode scopeNode)
    {
        Builder.PositionAtEnd(scope.LLVMBlock);

        foreach (StatementNode statement in scopeNode.Statements)
        {
            if (statement is ReturnNode @return)
            {
                if (@return.ReturnValue != null)
                {
                    Value expr = SafeLoad(CompileExpression(scope, @return.ReturnValue));

                    if (expr.Type.Equals(CurrentFunction.Type.ReturnType))
                    {
                        Builder.BuildRet(expr.LLVMValue);
                    }
                    else
                    {
                        throw new Exception($"Return value \"{expr.LLVMValue}\" does not match return type of function " +
                            $"\"{CurrentFunction.Type.Name}\" (\"{CurrentFunction.Type.ReturnType}\").");
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
                var newScope = new Scope(CurrentFunction.LLVMValue.AppendBasicBlock(""))
                {
                    Variables = new Dictionary<string, Variable>(scope.Variables),
                };
                Builder.BuildBr(newScope.LLVMBlock);
                Builder.PositionAtEnd(newScope.LLVMBlock);

                if (CompileScope(newScope, newScopeNode))
                {
                    return true;
                }

                scope.LLVMBlock = CurrentFunction.LLVMValue.AppendBasicBlock("");
                Builder.BuildBr(scope.LLVMBlock);
                Builder.PositionAtEnd(scope.LLVMBlock);
            }
            else if (statement is WhileNode @while)
            {
                LLVMBasicBlockRef loop = CurrentFunction.LLVMValue.AppendBasicBlock("loop");
                LLVMBasicBlockRef then = CurrentFunction.LLVMValue.AppendBasicBlock("then");
                LLVMBasicBlockRef @continue = CurrentFunction.LLVMValue.AppendBasicBlock("continue");
                Builder.BuildBr(loop);
                Builder.PositionAtEnd(loop);
                Value condition = CompileExpression(scope, @while.Condition);
                Builder.BuildCondBr(SafeLoad(condition).LLVMValue, then, @continue);
                Builder.PositionAtEnd(then);

                var newScope = new Scope(then)
                {
                    Variables = new Dictionary<string, Variable>(scope.Variables),
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
                Value condition = CompileExpression(scope, @if.Condition);
                LLVMBasicBlockRef then = CurrentFunction.LLVMValue.AppendBasicBlock("then");
                LLVMBasicBlockRef @else = CurrentFunction.LLVMValue.AppendBasicBlock("else");
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
                            Variables = new Dictionary<string, Variable>(scope.Variables),
                        };

                        if (CompileScope(newScope, @if.Then))
                        {
                            thenReturned = true;
                        }
                        else
                        {
                            if (@continue == null)
                            {
                                @continue = CurrentFunction.LLVMValue.AppendBasicBlock("continue");
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
                        Variables = new Dictionary<string, Variable>(scope.Variables),
                    };

                    if (@if.Else != null && CompileScope(newScope, @if.Else))
                    {
                        elseReturned = true;
                    }
                    else
                    {
                        if (@continue == null)
                        {
                            @continue = CurrentFunction.LLVMValue.AppendBasicBlock("continue");
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

    public Type ResolveType(TypeRefNode typeRef)
    {
        Type type;

        if (typeRef is GenericTypeRefNode)
        {
            throw new NotImplementedException();
        }
        else if (typeRef is FuncTypeRefNode fnTypeRef)
        {
            Type retType = ResolveType(fnTypeRef.ReturnType);
            var paramTypes = new List<Type>();

            foreach (TypeRefNode param in fnTypeRef.ParamterTypes)
            {
                Type paramType = ResolveType(param);
                paramTypes.Add(paramType);
            }

            return new LocalFuncType(retType, paramTypes.ToArray());
        }
        else
        {
            Struct @struct = GetStruct(UnVoid(typeRef));
            type = @struct;
        }

        int index = 0;

        while (index < typeRef.PointerDepth)
        {
            type = new PtrType(type);
            index++;
        }

        return type;
    }

    public Value CompileExpression(Scope scope, ExpressionNode expr)
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
        else if (expr is LocalFuncDefNode localFuncDef)
        {
            Type retType = ResolveType(localFuncDef.ReturnTypeRef);
            var @params = new List<Parameter>();
            var paramTypes = new List<Type>();
            uint index = 0;

            foreach (ParameterNode param in localFuncDef.Params)
            {
                Type paramType = ResolveParameter(param);
                paramTypes.Add(paramType);
                @params.Add(new Parameter(index, param.Name));
                index++;
            }

            var funcType = new LocalFuncType(retType, paramTypes.ToArray());
            LLVMValueRef llvmFunc = Module.AddFunction(funcType.Name, funcType.LLVMType);

            Function? parentFunction = CurrentFunction;
            var func = new Function(funcType, llvmFunc, @params.ToArray())
            {
                OpeningScope = new Scope(llvmFunc.AppendBasicBlock("entry"))
            };

            CurrentFunction = func;
            Value ret = !CompileScope(func.OpeningScope, localFuncDef.ExecutionBlock)
                ? throw new Exception("Local function is not guaranteed to return.")
                : new Value(funcType, llvmFunc);
            CurrentFunction = parentFunction;

            return ret;
        }
        else if (expr is InlineIfNode @if)
        {
            Value condition = CompileExpression(scope, @if.Condition);
            LLVMBasicBlockRef then = CurrentFunction.LLVMValue.AppendBasicBlock("then");
            LLVMBasicBlockRef @else = CurrentFunction.LLVMValue.AppendBasicBlock("else");
            LLVMBasicBlockRef @continue = CurrentFunction.LLVMValue.AppendBasicBlock("continue");

            //then
            Builder.PositionAtEnd(then);
            Value thenVal = CompileExpression(scope, @if.Then);

            //else
            Builder.PositionAtEnd(@else);
            Value elseVal = CompileExpression(scope, @if.Else);

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

            return thenVal.Type.Equals(elseVal.Type)
                ? new Value(WrapAsRef(thenVal.Type), result)
                : throw new Exception("Then and else statements of inline if are not of the same type.");
        }
        else if (expr is ConstantNode constNode)
        {
            return CompileLiteral(constNode);
        }
        else if (expr is InverseNode inverse)
        {
            Value @ref = CompileRef(scope, inverse.Value);
            return new Value(@ref.Type,
                Builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ,
                    SafeLoad(@ref).LLVMValue,
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0)));
        }
        else if (expr is IncrementVarNode incrementVar)
        {
            Value @ref = CompileRef(scope, incrementVar.RefNode);

            if (@ref.Type is not RefType || SafeLoad(@ref).Type is BasedType or FuncType)
            {
                throw new Exception(); //TODO
            }
            
            var valToAdd = LLVMValueRef.CreateConstInt(SafeLoad(@ref).Type.LLVMType, 1); //TODO: float compat?
            Builder.BuildStore(Builder.BuildAdd(SafeLoad(@ref).LLVMValue, valToAdd), @ref.LLVMValue);
            return new Value(WrapAsRef(@ref.Type), @ref.LLVMValue);
        }
        else if (expr is DecrementVarNode decrementVar)
        {
            Value @ref = CompileRef(scope, decrementVar.RefNode);

            if (@ref.Type is not RefType || SafeLoad(@ref).Type is BasedType or FuncType)
            {
                throw new Exception(); //TODO
            }
            
            var valToSub = LLVMValueRef.CreateConstInt(SafeLoad(@ref).Type.LLVMType, 1); //TODO: float compat?
            Builder.BuildStore(Builder.BuildSub(SafeLoad(@ref).LLVMValue, valToSub), @ref.LLVMValue);
            return new Value(WrapAsRef(@ref.Type), @ref.LLVMValue);
        }
        else if (expr is AsReferenceNode asReference)
        {
            Value value = CompileExpression(scope, asReference.Value);
            LLVMValueRef newVal = Builder.BuildAlloca(value.Type.LLVMType);
            Builder.BuildStore(value.LLVMValue, newVal);
            return new Value(new PtrType(value.Type), newVal);
        }
        else if (expr is DeReferenceNode deReference)
        {
            Value value = SafeLoad(CompileExpression(scope, deReference.Value));

            return value.Type is PtrType ptrType
                ? new Value(ptrType.BaseType, Builder.BuildLoad2(ptrType.BaseType.LLVMType, value.LLVMValue))
                : throw new Exception("Attempted to load a non-pointer.");
        }
        else
        {
            return expr is RefNode @ref
                ? CompileRef(scope, @ref)
                : expr is SubExprNode subExpr
                    ? CompileExpression(scope, subExpr.Expression)
                    : throw new NotImplementedException();
        }
    }

    public Value CompileLiteral(ConstantNode constNode)
    {
        if (constNode.Value is string str)
        {
            Struct @struct = GetStruct(Reserved.Char);
            LLVMValueRef constStr = Context.GetConstString(str, false);
            LLVMValueRef global = Module.AddGlobal(constStr.TypeOf, "litstr");
            global.Initializer = constStr;
            return new Value(new PtrType(@struct), global);
        }
        else if (constNode.Value is bool @bool)
        {
            Class @class = Primitives.Bool;
            return new Value(@class, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, (ulong)(@bool ? 1 : 0)));
        }
        else if (constNode.Value is int i32)
        {
            Class @class = Primitives.Int32;
            return new Value(@class, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)i32, true));
        }
        else if (constNode.Value is float f32)
        {
            Class @class = Float.Float32;
            return new Value(@class, LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, f32));
        }
        else if (constNode.Value is char ch)
        {
            Class @class = Primitives.Char;
            return new Value(@class, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, ch));
        }
        else if (constNode.Value == null)
        {
            Class @class = Primitives.Char;
            return new Value(@class, LLVMValueRef.CreateConstNull(@class.LLVMType));
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public Value CompileOperation(Scope scope, BinaryOperationNode binaryOp)
    {
        Value left = SafeLoad(CompileExpression(scope, binaryOp.Left));
        Value right = SafeLoad(CompileExpression(scope, binaryOp.Right));

        if (binaryOp.Type == OperationType.Exponential
            && right.Type is Float or Int
            && left.Type is Float or Int)
        {
            return CompilePow(left, right);
        }
        else if (left.Type.Equals(right.Type)
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
                    builtVal = left.Type is Float
                        ? Builder.BuildFAdd(leftVal, rightVal)
                        : Builder.BuildAdd(leftVal, rightVal);
                    break;
                case OperationType.Subtraction:
                    builtVal = left.Type is Float
                        ? Builder.BuildFSub(leftVal, rightVal)
                        : Builder.BuildSub(leftVal, rightVal);
                    break;
                case OperationType.Multiplication:
                    builtVal = left.Type is Float
                        ? Builder.BuildFMul(leftVal, rightVal)
                        : Builder.BuildMul(leftVal, rightVal);
                    break;
                case OperationType.Division:
                    builtVal = left.Type is Float
                        ? Builder.BuildFDiv(leftVal, rightVal)
                        : left.Type is UnsignedInt
                            ? Builder.BuildUDiv(leftVal, rightVal)
                            : left.Type is SignedInt
                                ? Builder.BuildSDiv(leftVal, rightVal)
                                : throw new NotImplementedException();

                    break;
                case OperationType.Modulo:
                    builtVal = left.Type is Float
                        ? Builder.BuildFRem(leftVal, rightVal)
                        : left.Type is UnsignedInt
                            ? Builder.BuildURem(leftVal, rightVal)
                            : left.Type is SignedInt
                                ? Builder.BuildSRem(leftVal, rightVal)
                                : throw new NotImplementedException();

                    break;
                case OperationType.And:
                    builtVal = Builder.BuildAnd(leftVal, rightVal);
                    builtType = Primitives.Bool;
                    break;
                case OperationType.Or:
                    builtVal = Builder.BuildOr(leftVal, rightVal);
                    builtType = Primitives.Bool;
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
                            : left.Type is Float
                                ? Builder.BuildFCmp(binaryOp.Type == OperationType.Equal
                                        ? LLVMRealPredicate.LLVMRealOEQ
                                        : LLVMRealPredicate.LLVMRealUNE,
                                    leftVal, rightVal)
                                : left.Type is Int
                                    ? Builder.BuildICmp(binaryOp.Type == OperationType.Equal
                                            ? LLVMIntPredicate.LLVMIntEQ
                                            : LLVMIntPredicate.LLVMIntNE,
                                        leftVal, rightVal)
                                    : throw new NotImplementedException();

                    builtType = Primitives.Bool;
                    break;
                case OperationType.GreaterThan:
                case OperationType.GreaterThanOrEqual:
                case OperationType.LesserThan:
                case OperationType.LesserThanOrEqual:
                    builtVal = left.Type is Float
                        ? Builder.BuildFCmp(binaryOp.Type switch
                        {
                            OperationType.GreaterThan => LLVMRealPredicate.LLVMRealOGT,
                            OperationType.GreaterThanOrEqual => LLVMRealPredicate.LLVMRealOGE,
                            OperationType.LesserThan => LLVMRealPredicate.LLVMRealOLT,
                            OperationType.LesserThanOrEqual => LLVMRealPredicate.LLVMRealOLE,
                            _ => throw new NotImplementedException(),
                        }, leftVal, rightVal)
                        : left.Type is UnsignedInt
                            ? Builder.BuildICmp(binaryOp.Type switch
                            {
                                OperationType.GreaterThan => LLVMIntPredicate.LLVMIntUGT,
                                OperationType.GreaterThanOrEqual => LLVMIntPredicate.LLVMIntUGE,
                                OperationType.LesserThan => LLVMIntPredicate.LLVMIntULT,
                                OperationType.LesserThanOrEqual => LLVMIntPredicate.LLVMIntULE,
                                _ => throw new NotImplementedException(),
                            }, leftVal, rightVal)
                            : left.Type is SignedInt
                                ? Builder.BuildICmp(binaryOp.Type switch
                                {
                                    OperationType.GreaterThan => LLVMIntPredicate.LLVMIntSGT,
                                    OperationType.GreaterThanOrEqual => LLVMIntPredicate.LLVMIntSGE,
                                    OperationType.LesserThan => LLVMIntPredicate.LLVMIntSLT,
                                    OperationType.LesserThanOrEqual => LLVMIntPredicate.LLVMIntSLE,
                                    _ => throw new NotImplementedException(),
                                }, leftVal, rightVal)
                                : throw new NotImplementedException($"Unimplemented comparison between {left.Type} " +
                                    $"and {right.Type}.");

                    builtType = Primitives.Bool;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return new Value(builtType, builtVal);
        }
        else
        {
            throw new Exception($"Operation cannot be done with operands of types \"{left.Type}\" " +
                $"and \"{right.Type}\"!");
        }
    }

    public Value CompileCast(Scope scope, BinaryOperationNode binaryOp)
    {
        if (binaryOp.Left is not TypeRefNode left)
        {
            throw new Exception($"Cast destination (\"{binaryOp.Left}\") is invalid.");
        }

        Value right = SafeLoad(CompileExpression(scope, binaryOp.Right));
        Type destType = ResolveType(left);
        LLVMValueRef builtVal = destType is Int
            ? right.Type is Int
                ? destType.Equals(Primitives.Bool)
                    ? Builder.BuildICmp(LLVMIntPredicate.LLVMIntNE,
                        LLVMValueRef.CreateConstInt(right.Type.LLVMType, 0), right.LLVMValue)
                    : right.Type.Equals(Primitives.Bool)
                        ? Builder.BuildZExt(right.LLVMValue, destType.LLVMType)
                        : Builder.BuildIntCast(right.LLVMValue, destType.LLVMType)
                : right.Type is Float
                    ? destType is UnsignedInt
                        ? Builder.BuildFPToUI(right.LLVMValue, destType.LLVMType)
                        : destType is SignedInt
                            ? Builder.BuildFPToSI(right.LLVMValue, destType.LLVMType)
                            : throw new NotImplementedException()
                    : throw new NotImplementedException()
            : destType is Float
                ? right.Type is Float
                    ? Builder.BuildFPCast(right.LLVMValue, destType.LLVMType)
                    : right.Type is Int
                        ? right.Type is UnsignedInt
                            ? Builder.BuildUIToFP(right.LLVMValue, destType.LLVMType)
                            : right.Type is SignedInt
                                ? Builder.BuildSIToFP(right.LLVMValue, destType.LLVMType)
                                : throw new NotImplementedException()
                        : throw new NotImplementedException()
                : Builder.BuildCast(LLVMOpcode.LLVMBitCast,
                    right.LLVMValue,
                    destType.LLVMType);
        return new Value(destType, builtVal);
    }

    public Value CompilePow(Value left, Value right)
    {
        Class i16 = Primitives.Int16;
        Class i32 = Primitives.Int32;
        Class i64 = Primitives.Int64;
        Class f32 = Float.Float32;
        Class f64 = Float.Float64;

        LLVMValueRef val;
        string intrinsic;
        LLVMTypeRef destType = LLVMTypeRef.Float;
        bool returnInt = left.Type is Int && right.Type is Int;

        if (left.Type is Int)
        {
            if (left.Type.Equals(Primitives.UInt64)
                || left.Type.Equals(Primitives.Int64))
            {
                destType = LLVMTypeRef.Double;
            }

            val = left.Type is SignedInt
                ? Builder.BuildSIToFP(SafeLoad(left).LLVMValue, destType)
                : left.Type is UnsignedInt
                    ? Builder.BuildUIToFP(SafeLoad(left).LLVMValue, destType)
                    : throw new NotImplementedException();
            left = new Value(val.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
                    ? f64
                    : f32,
                val);
        }
        else if (left.Type is Float
            && !left.Type.Equals(Float.Float64))
        {
            val = Builder.BuildFPCast(SafeLoad(left).LLVMValue, destType);
            left = new Value(val.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
                    ? f64
                    : f32,
                val);
        }
        else
        {
            throw new NotImplementedException();
        }

        if (right.Type is Float)
        {
            if (left.Type.Equals(Float.Float64))
            {
                if (right.Type.Equals(Float.Float64))
                {
                    val = Builder.BuildFPCast(right.LLVMValue, LLVMTypeRef.Double);
                    right = new Value(f64, val);
                }

                intrinsic = "llvm.pow.f64";
            }
            else
            {
                if (right.Type.Equals(Float.Float32))
                {
                    val = Builder.BuildFPCast(right.LLVMValue, LLVMTypeRef.Float);
                    right = new Value(f32, val);
                }

                intrinsic = "llvm.pow.f32";
            }
        }
        else if (right.Type is Int)
        {
            if (left.Type.Equals(Float.Float64))
            {
                if (!right.Type.Equals(Primitives.UInt16)
                    && !right.Type.Equals(Primitives.Int16))
                {
                    val = Builder.BuildIntCast(right.LLVMValue, LLVMTypeRef.Int16);
                    right = new Value(i16, val);
                }

                intrinsic = "llvm.powi.f64.i16";
            }
            else
            {
                if (right.Type.Equals(Primitives.UInt32)
                    || right.Type.Equals(Primitives.Int32))
                {
                    val = Builder.BuildIntCast(right.LLVMValue, LLVMTypeRef.Int32);
                    right = new Value(i32, val);
                }

                intrinsic = "llvm.powi.f32.i32";
            }
        }
        else
        {
            throw new NotImplementedException();
        }

        IntrinsicFunction func = GetIntrinsic(intrinsic);
        var args = new Value[2]
        {
            SafeLoad(left),
            SafeLoad(right),
        };
        Value result = func.Call(this, args);

        if (returnInt)
        {
            result = result.LLVMValue.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
                ? new Value(i64,
                    Builder.BuildFPToSI(result.LLVMValue,
                        LLVMTypeRef.Int64))
                : new Value(i32,
                    Builder.BuildFPToSI(result.LLVMValue,
                        LLVMTypeRef.Int32));
        }

        return result;
    }

    public Value CompileAssignment(Scope scope, BinaryOperationNode binaryOp)
    {
        Value variableAssigned = CompileExpression(scope, binaryOp.Left); //TODO: does not work with arrays

        if (variableAssigned.Type is not BasedType varType)
        {
            throw new Exception($"Cannot assign to \"{variableAssigned.LLVMValue.PrintToString()}\" " +
                $"as it is not a pointer.");
        }

        Value value = SafeLoad(CompileExpression(scope, binaryOp.Right));

        if (!varType.BaseType.Equals(value.Type))
        {
            throw new Exception($"Tried to assign value of type \"{value.Type}\" " +
                $"to variable of type \"{varType.BaseType}\". " +
                $"Left: \"{binaryOp.Left.GetDebugString()}\". Right: \"{binaryOp.Right.GetDebugString()}\".");
        }

        Builder.BuildStore(value.LLVMValue, variableAssigned.LLVMValue);
        return new Value(WrapAsRef(variableAssigned.Type), variableAssigned.LLVMValue);
    }

    public Value CompileLocal(Scope scope, LocalDefNode localDef)
    {
        Value? value = null;
        Type type;

        if (localDef is InferredLocalDefNode inferredLocalDef)
        {
            value = CompileExpression(scope, inferredLocalDef.Value);
            type = value.Type;
        }
        else
        {
            type = ResolveType(localDef.TypeRef);
        }

        LLVMValueRef llvmVariable = Builder.BuildAlloca(type.LLVMType, localDef.Name); //TODO: crashes with a func type, if on stack
        scope.Variables.Add(localDef.Name, new Variable(localDef.Name, type, llvmVariable));

        if (value != null)
        {
            Builder.BuildStore(SafeLoad(value).LLVMValue, llvmVariable);
        }

        return new Value(WrapAsRef(type), llvmVariable);
    }

    public Value CompileRef(Scope scope, RefNode? refNode)
    {
        Value? context = null;

        while (refNode != null)
        {
            if (refNode is ThisNode)
            {
                if (CurrentFunction.OwnerStruct == null)
                {
                    throw new Exception("Attempted self-instance reference in a global function.");
                }

                if (CurrentFunction.Type.Name == Reserved.Init)
                {
                    context = scope.Variables[Reserved.Self];
                }
                else
                {
                    context = new Value(WrapAsRef(CurrentFunction.OwnerStruct), CurrentFunction.LLVMValue.FirstParam);
                }

                refNode = refNode.Child;
            }
            else if (refNode is TypeRefNode typeRef)
            {
                Struct @struct = GetStruct(UnVoid(typeRef));
                refNode = refNode.Child;

                if (refNode is FuncCallNode funcCall)
                {
                    context = CompileFuncCall(context, scope, funcCall, @struct);
                    refNode = refNode.Child;
                }
                else
                {
                    throw new Exception($"#{@struct} cannot be treated like an expression value.");
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

                context = new Value(new RefType(resultType),
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

        return context ?? throw new Exception($"Failed to compile \"{refNode}\".");
    }

    public Value CompileVarRef(Value? context, Scope scope, RefNode refNode)
    {
        if (context != null)
        {
            if (context.Type is not Class classType)
            {
                throw new Exception(); //TODO
            }
            
            Field field = classType.Fields[refNode.Name]; //TODO: use the GetField() method on the class instead
            Type type = SafeLoad(context).Type;
            return new Value(WrapAsRef(field.Type),
                Builder.BuildStructGEP2(type.LLVMType,
                    context.LLVMValue,
                    field.FieldIndex,
                    field.Name));
        }
        else
        {
            if (scope.Variables.TryGetValue(refNode.Name, out Variable @var))
            {
                return @var;
            }
            else
            {
                throw new Exception($"Variable \"{refNode.Name}\" does not exist.");
            }
        }
    }

    public Value CompileFuncCall(Value? context, Scope scope, FuncCallNode funcCall, Struct? sourceStruct = null)
    { //TODO: needs to be reworked
        Function? func;
        var argTypes = new List<Type>();
        var args = new List<Value>();
        bool contextWasNull = context == null;

        if (context == null)
        {
            if (CurrentFunction.OwnerStruct != null)
            {
                context = new Value(CurrentFunction.OwnerStruct, CurrentFunction.LLVMValue.FirstParam);
            }
        }

        foreach (ExpressionNode arg in funcCall.Arguments)
        {
            Value val = CompileExpression(scope, arg);
            argTypes.Add(val.Type is RefType @ref ? @ref.BaseType : val.Type);
            args.Add(SafeLoad(val));
        }

        var sig = new Signature(funcCall.Name, argTypes);

        if (context != null && context.Type is Class classType && classType.Methods.TryGetValue(sig, out func))
        {
            if (context.Type.LLVMType == func.Type.LLVMType.ParamTypes[0])
            {
                var newArgs = new List<Value> { context };
                newArgs.AddRange(args);
                args = newArgs;
            }
            else
            {
                throw new Exception("Attempted to call a method on a different class. " +
                    "!!THIS IS NOT A USER ERROR. REPORT ASAP!!");
            }
        }
        else if (contextWasNull && CurrentNamespace.Functions.TryGetValue(sig, out func))
        {
            // Keep empty
        }
        else if (sourceStruct != null && sourceStruct.StaticMethods.TryGetValue(sig, out func))
        {
            // Keep empty
        }
        else
        {
            throw new Exception($"Function \"{funcCall.Name}\" does not exist.");
        }

        return func.Call(this, args.ToArray());
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
                    LLVMValueRef llvmFunc = func.LLVMValue;
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

    public Namespace GetNamespace(string name)
    {
        if (CurrentNamespace.Name == name)
        {
            return CurrentNamespace;
        }
        else if (CurrentNamespace.TryGetNamespace(name, out Namespace nmspace))
        {
            return nmspace;
        }
        else if (_imports.TryGetNamespace(name, out nmspace))
        {
            return nmspace;
        }
        else
        {
            throw new Exception($"Could not find namespace \"{name}\".");
        }
    }

    public Struct GetStruct(string name)
    {
        if (CurrentNamespace.TryGetStruct(name, out Struct @struct))
        {
            return @struct;
        }
        else if (_imports.TryGetStruct(name, out @struct))
        {
            return @struct;
        }
        else
        {
            throw new Exception($"Could not find type \"{name}\".");
        }
    }

    public Function GetFunction(Signature sig)
    {
        if (CurrentNamespace.TryGetFunction(sig, out Function func))
        {
            return func;
        }
        else if (_imports.TryGetFunction(sig, out func))
        {
            return func;
        }
        else
        {
            throw new Exception($"Could not find function \"{sig}\".");
        }
    }

    public Value SafeLoad(Value value)
    {
        return value.Type is RefType @ref && !value.Type.Equals(Primitives.Void)
            ? new Value(@ref.BaseType, Builder.BuildLoad2(@ref.BaseType.LLVMType, value.LLVMValue))
            : value;
    }

    public RefType WrapAsRef(Type type)
        => type is RefType @ref
            ? @ref
            : new RefType(type);

    private Namespace InitGlobalNamespace()
    {
        var @namespace = new Namespace(null, "global_compiler");

        @namespace.Structs.Add(Reserved.Void, Primitives.Void);
        @namespace.Structs.Add(Reserved.Float16, Float.Float16);
        @namespace.Structs.Add(Reserved.Float32, Float.Float32);
        @namespace.Structs.Add(Reserved.Float64, Float.Float64);
        @namespace.Structs.Add(Reserved.Bool, Primitives.Bool);
        @namespace.Structs.Add(Reserved.Char, Primitives.Char);
        @namespace.Structs.Add(Reserved.UnsignedInt8, Primitives.UInt8);
        @namespace.Structs.Add(Reserved.UnsignedInt16, Primitives.UInt16);
        @namespace.Structs.Add(Reserved.UnsignedInt32, Primitives.UInt32);
        @namespace.Structs.Add(Reserved.UnsignedInt64, Primitives.UInt64);
        @namespace.Structs.Add(Reserved.SignedInt8, Primitives.Int8);
        @namespace.Structs.Add(Reserved.SignedInt16, Primitives.Int16);
        @namespace.Structs.Add(Reserved.SignedInt32, Primitives.Int32);
        @namespace.Structs.Add(Reserved.SignedInt64, Primitives.Int64);

        foreach (Class @class in @namespace.Structs.Values)
        {
            @class.AddBuiltins(this);
        }

        return @namespace;
    }

    private IntrinsicFunction CreateIntrinsic(string name)
    {
        Pow func = name switch
        {
            "llvm.powi.f32.i32" => new Pow(name, Module, Float.Float32, Float.Float32, Primitives.Int32),
            "llvm.powi.f64.i16" => new Pow(name, Module, Float.Float64, Float.Float64, Primitives.Int64),
            "llvm.pow.f32" => new Pow(name, Module, Float.Float32, Float.Float32, Float.Float32),
            "llvm.pow.f64" => new Pow(name, Module, Float.Float64, Float.Float64, Float.Float64),
            _ => throw new NotImplementedException($"Intrinsic \"{name}\" is not implemented."),
        };

        _intrinsics.Add(name, func);
        return func;
    }
    
    private void OpenFile(string @namespace, string[] imports)
    {
        CurrentNamespace = ResolveNamespace(@namespace);
        _imports = ResolveImports(imports);
    }
}
