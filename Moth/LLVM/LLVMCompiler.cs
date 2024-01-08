using Moth.AST;
using Moth.AST.Node;
using Moth.LLVM.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using Array = Moth.LLVM.Data.Array;
using Pointer = Moth.LLVM.Data.Pointer;

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
    public List<Struct> Types { get; } = new List<Struct>();
    public List<DefinedFunction> Functions { get; } = new List<DefinedFunction>();
    public List<IGlobal> Globals { get; } = new List<IGlobal>();

    public Func<string, IReadOnlyList<object>, IAttribute> MakeAttribute { get; }

    private readonly Logger _logger = new Logger("moth/compiler");
    private readonly Dictionary<string, IntrinsicFunction> _intrinsics = new Dictionary<string, IntrinsicFunction>();
    private readonly Dictionary<string, FuncType> _foreigns = new Dictionary<string, FuncType>();
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
        AddDefaultForeigns();

        if (doOptimize)
        {
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
        
        Log("Registering compiler attributes...");
        MakeAttribute = IAttribute.MakeCreationFunction(Assembly.GetExecutingAssembly().GetTypes().ToArray());
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
                _currentFunction = value.Type is FuncType
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
        string[] names = str.Split("::");
        uint index = 0;
        
        foreach (var name in names) //TODO: is this unnecessary?
        {
            names[index] = name.Replace("::", "");
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
    
    public unsafe void LoadLibrary(string path)
    {
        var module = LoadLLVMModule(path);
        var match = Regex.Match(Path.GetFileName(path),
            "(.*)(?=\\.mothlib)");
        
        if (!match.Success)
        {
            throw new Exception($"Cannot load mothlibs, \"{path}\" does not have the correct extension.");
        }

        var libName = match.Value;
        var global = module.GetNamedGlobal($"<{libName}/metadata>");
        var s = global.GetMDString(out uint len);
        match = Regex.Match(global.ToString(), "(?<=<metadata>)(.*)(?=<\\/metadata>)");

        if (!match.Success)
        {
            throw new Exception($"Cannot load mothlibs, missing metadata for \"{libName}\".");
        }

        var metadata = new MemoryStream(Utils.Unescape(match.Value), false);
        var deserializer = new MetadataDeserializer(this, metadata);
        deserializer.Process(libName);
    }

    public unsafe byte[] GenerateMetadata(string assemblyName)
    {
        MemoryStream bytes = new MemoryStream();
        MetadataSerializer serializer = new MetadataSerializer(this, bytes);
        bytes.Write(System.Text.Encoding.UTF8.GetBytes("<metadata>"));
        serializer.Process();
        bytes.Write(System.Text.Encoding.UTF8.GetBytes("</metadata>"));
        var global = Module.AddGlobal(LLVMTypeRef.CreateArray(LLVMTypeRef.Int8,
                (uint)bytes.Length),
            $"<{assemblyName}/metadata>");
        global.Initializer = LLVMValueRef.CreateConstArray(LLVMTypeRef.Int8,
            bytes.ToArray().AsLLVMValues());
        global.Linkage = LLVMLinkage.LLVMDLLExportLinkage;
        global.IsGlobalConstant = true;
        return bytes.ToArray();
    }
    
    public LLVMCompiler Compile(IReadOnlyCollection<ScriptAST> scripts)
    {
        foreach (ScriptAST script in scripts)
        {
            OpenFile(script.Namespace, script.Imports.ToArray());

            foreach (ClassNode classNode in script.ClassNodes)
            {
                if (classNode is GenericClassNode genericClass)
                {
                    CurrentNamespace.GenericClassTemplates.Add(genericClass.Name, genericClass);
                }
                else
                {
                    DefineType(classNode);
                }
            }
            
            foreach (FieldDefNode global in script.GlobalVariables)
            {
                DefineGlobal(global);
            }

            foreach (FuncDefNode funcDefNode in script.GlobalFunctions)
            {
                DefineFunction(funcDefNode);
            }

            foreach (ClassNode classNode in script.ClassNodes)
            {
                if (classNode is not GenericClassNode)
                {
                    Struct @struct = GetStruct(classNode.Name);

                    if (classNode.Scope != null)
                    {
                        foreach (FuncDefNode funcDefNode in classNode.Scope.Statements.OfType<FuncDefNode>())
                        {
                            DefineFunction(funcDefNode, @struct);
                        }
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
                    CompileType(@class);
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

                    if (classNode.Scope != null)
                    {
                        foreach (FuncDefNode funcDefNode in classNode.Scope.Statements.OfType<FuncDefNode>())
                        {
                            CompileFunction(funcDefNode, @struct);
                        }
                    }
                }
            }
        }

        return this;
    }

    public void DefineType(ClassNode classNode)
    {
        Struct newStruct;
        
        if (classNode.IsStruct)
        {
            if (classNode.Scope == null)
            {
                newStruct = new OpaqueStruct(this,
                    CurrentNamespace,
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
        Types.Add(newStruct.AddBuiltins(this));
    }

    public void CompileType(ClassNode classNode)
    {
        var llvmTypes = new List<LLVMTypeRef>();
        Struct @struct = GetStruct(classNode.Name);

        if (@struct is OpaqueStruct)
        {
            return;
        }
        
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
        var attributes = new Dictionary<string, IAttribute>();
        
        foreach (AttributeNode attribute in funcDefNode.Attributes)
        {
            attributes.Add(attribute.Name, MakeAttribute(attribute.Name, CleanAttributeArgs(attribute.Arguments.ToArray())));
        }

        if (attributes.TryGetValue(Reserved.TargetOS, out var attr))
        {
            if (!((TargetOSAttribute)attr).Targets.Contains(Utils.GetOS())) return;
        }
        
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
            paramNode.TypeRef.Name = paramNode.TypeRef.Name;
            @params.Add(new Parameter(index, paramNode.Name));
            paramTypes.Add(paramType);
            index++;
        }

        var sig = new Signature(funcDefNode.Name, paramTypes, funcDefNode.IsVariadic);
        string funcName = funcDefNode.Name;
        var builder = new StringBuilder("(");
        
        foreach (var type in paramTypes)
        {
            builder.Append($"{type}, ");
        }

        if (paramTypes.Count > 0)
        {
            builder.Remove(builder.Length - 2, 2);
        }

        builder.Append(')');
        
        Type returnType = ResolveType(funcDefNode.ReturnTypeRef);
        LLVMTypeRef llvmFuncType = LLVMTypeRef.CreateFunction(returnType.LLVMType,
            paramTypes.AsLLVMTypes().ToArray(),
            funcDefNode.IsVariadic);
        FuncType funcType = @struct == null
            ? new FuncType(returnType, paramTypes.ToArray(), funcDefNode.IsVariadic)
            : new MethodType(returnType, paramTypes.ToArray(), @struct, funcDefNode.IsStatic);
        DefinedFunction func = new DefinedFunction(this,
            @struct == null
                ? CurrentNamespace
                : @struct,
            funcName,
            funcType,
            @params.ToArray(),
            funcDefNode.Privacy,
            funcDefNode.IsForeign,
            attributes);
        
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
                throw new Exception($"Cannot have instance method \"{func.Name}\" on struct \"{@struct.Name}\"");
            }
        }
        else
        {
            CurrentNamespace.Functions.Add(sig, func);
        }
        
        Functions.Add(func);
    }

    public void CompileFunction(FuncDefNode funcDefNode, Struct? @struct = null)
    {
        // Confirm that the definition is for the correct OS
        {
            foreach (AttributeNode attribute in funcDefNode.Attributes)
            {
                if (attribute.Name == Reserved.TargetOS)
                {
                    var targetOS = (TargetOSAttribute)MakeAttribute(attribute.Name, CleanAttributeArgs(attribute.Arguments.ToArray()));
                    if (!targetOS.Targets.Contains(Utils.GetOS())) return;
                }
            }
        }
        
        Function? func;
        var paramTypes = new List<Type>();

        foreach (ParameterNode param in funcDefNode.Params)
        {
            paramTypes.Add(ResolveParameter(param));
        }

        if (!funcDefNode.IsStatic && @struct != null)
        {
            var newParamTypes = new List<Type>()
            {
                new PtrType(@struct)
            };
            
            newParamTypes.AddRange(paramTypes);
            paramTypes = newParamTypes;
        }
        
        var sig = new Signature(funcDefNode.Name, paramTypes);

        if (funcDefNode.IsForeign
            || funcDefNode.ExecutionBlock == null)
        {
            return;
        }
        else if (@struct != null
            && @struct is Class @class
            && !funcDefNode.IsStatic
            && @class.Methods.TryGetValue(sig, out func))
        {
            // Keep empty
        }
        else if (@struct != null
            && funcDefNode.IsStatic
            && @struct.StaticMethods.TryGetValue(sig, out func))
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

        if (funcDefNode.Name == Reserved.Init && func.IsStatic && func.Type is MethodType methodType)
        {
            if (func.Type.ReturnType is Class retType)
            {
                if (retType != methodType.OwnerStruct)
                {
                    throw new Exception($"Init method does not return the same type as its owner class " +
                        $"(\"{methodType.OwnerStruct.Name}\").");
                }
            }

            var @new = methodType.OwnerStruct.Init(this);
            func.OpeningScope.Variables.Add(@new.Name, @new);

            if (!(@new.Type.BaseType is PtrType ptrType && ptrType.BaseType is Class classOfNew))
            {
                throw new Exception($"Critical failure in the init of class \"{methodType.OwnerStruct}\".");
            }
            
            foreach (Field field in classOfNew.Fields.Values)
            {
                LLVMValueRef llvmField = Builder.BuildStructGEP2(methodType.OwnerStruct.LLVMType, @new.LLVMValue, field.FieldIndex);
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
                Log($"(unsafe) Running optimization pass on function \"{func.FullName}\".");
            
                unsafe
                {
                    //LLVMSharp.Interop.LLVM.RunFunctionPassManager(FunctionPassManager, func.LLVMValue);
                }
            }
        }
        else
        {
            throw new Exception("Function is not guaranteed to return.");
        }
    }

    public LLVMValueRef HandleForeign(string funcName, FuncType funcType)
    {
        if (_foreigns.TryGetValue(funcName, out FuncType prevType))
        {
            if (!prevType.Equals(funcType))
            {
                throw new Exception($"Foreign function \"{funcType}\" cannot overload other definition \"{prevType}\".");
            }

            return Module.GetNamedFunction(funcName);
        }
        else
        {
            _foreigns.Add(funcName, funcType);
            return Module.AddFunction(funcName, funcType.BaseType.LLVMType);
        }
    }
    
    public Type ResolveParameter(ParameterNode param)
    {
        return ResolveType(param.TypeRef);
    }

    public void DefineGlobal(FieldDefNode globalDef, Class? @class = null)
    {
        Type globalType = ResolveType(globalDef.TypeRef);
        LLVMValueRef globalVal = Module.AddGlobal(globalType.LLVMType, globalDef.Name);
        GlobalVariable global = new GlobalVariable(globalDef.Name, WrapAsRef(globalType), globalVal, globalDef.Privacy);
        CurrentNamespace.GlobalVariables.Add(globalDef.Name, global);
        Globals.Add(global);
        //TODO: add const support to globals
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
                        throw new Exception($"Return value \"{expr.LLVMValue}\" " +
                            $"does not match return type of function " +
                            $"\"{CurrentFunction.Name}\" (\"{CurrentFunction.Type.ReturnType}\").");
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
        else if (typeRef is ArrayTypeRefNode arrayTypeRef)
        {
            Type elementType = ResolveType(arrayTypeRef.ElementType);
            type = new ArrType(this, elementType);
        }
        else
        {
            Struct @struct = GetStruct(typeRef.Name);
            type = @struct;
        }
        
        for (int i = 0; i < typeRef.PointerDepth; i++) //TODO: confirm it works
        {
            type = new PtrType(type);
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
            LLVMValueRef llvmFunc = Module.AddFunction(Reserved.LocalFunc, funcType.BaseType.LLVMType);

            Function? parentFunction = CurrentFunction;
            var func = new Function(funcType, llvmFunc, @params.ToArray())
            {
                OpeningScope = new Scope(llvmFunc.AppendBasicBlock("entry"))
            };

            CurrentFunction = func;
            Value ret = !CompileScope(func.OpeningScope, localFuncDef.ExecutionBlock)
                ? throw new Exception("Local function is not guaranteed to return.")
                : func;
            CurrentFunction = parentFunction;
            
            Builder.PositionAtEnd(scope.LLVMBlock);
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

            return SafeLoad(thenVal).Type.Equals(SafeLoad(elseVal).Type)
                ? Value.Create(WrapAsRef(thenVal.Type), result)
                : throw new Exception("Then and else statements of inline if are not of the same type.");
        }
        else if (expr is LiteralArrayNode literalArrayNode)
        {
            return new Array(this,
                ResolveType(literalArrayNode.ElementType),
                literalArrayNode.Elements.CompileToValues(this, scope));
        }
        else if (expr is LiteralNode literalNode)
        {
            return CompileLiteral(literalNode);
        }
        else if (expr is InverseNode inverse)
        {
            Value @ref = CompileRef(scope, inverse.Value);
            return Value.Create(@ref.Type,
                Builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ,
                    SafeLoad(@ref).LLVMValue,
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0)));
        }
        else if (expr is IncrementVarNode incrementVar)
        {
            Value @ref = CompileRef(scope, incrementVar.RefNode);

            if (@ref is not Pointer ptr || @ref.Type is not RefType || SafeLoad(@ref).Type is PtrType or FuncType)
            {
                throw new Exception(); //TODO
            }
            
            var valToAdd = LLVMValueRef.CreateConstInt(SafeLoad(@ref).Type.LLVMType, 1); //TODO: float compat?
            ptr.Store(this, Value.Create(ptr.Type.BaseType, Builder.BuildAdd(SafeLoad(@ref).LLVMValue, valToAdd)));
            return new Pointer(WrapAsRef(@ref.Type), @ref.LLVMValue);
        }
        else if (expr is DecrementVarNode decrementVar)
        {
            Value @ref = CompileRef(scope, decrementVar.RefNode);

            if (@ref is not Pointer ptr || @ref.Type is not RefType || SafeLoad(@ref).Type is PtrType or FuncType)
            {
                throw new Exception(); //TODO
            }
            
            var valToAdd = LLVMValueRef.CreateConstInt(SafeLoad(@ref).Type.LLVMType, 1); //TODO: float compat?
            ptr.Store(this, Value.Create(ptr.Type.BaseType, Builder.BuildSub(SafeLoad(@ref).LLVMValue, valToAdd)));
            return new Pointer(WrapAsRef(@ref.Type), @ref.LLVMValue);
        }
        else if (expr is AddressOfNode addrofNode)
        {
            Value value = CompileExpression(scope, addrofNode.Value);
            return value.GetAddr(this);
        }
        else if (expr is LoadNode loadNode)
        {
            Value value = SafeLoad(CompileExpression(scope, loadNode.Value));

            return value is Pointer ptr
                ? ptr.Load(this)
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

    public Value CompileLiteral(LiteralNode literalNode)
    {
        if (literalNode.Value is string str)
        {
            Struct @struct = GetStruct(Reserved.Char);
            LLVMValueRef constStr = Context.GetConstString(str, false);
            LLVMValueRef global = Module.AddGlobal(constStr.TypeOf, "litstr");
            global.Initializer = constStr;
            return Value.Create(new PtrType(@struct), global);
        }
        else if (literalNode.Value is bool @bool)
        {
            Struct @struct = Primitives.Bool;
            return Value.Create(@struct, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, (ulong)(@bool ? 1 : 0)));
        }
        else if (literalNode.Value is int i32)
        {
            Struct @struct = Primitives.Int32;
            return Value.Create(@struct, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)i32, true));
        }
        else if (literalNode.Value is float f32)
        {
            Struct @struct = Primitives.Float32;
            return Value.Create(@struct, LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, f32));
        }
        else if (literalNode.Value is char ch)
        {
            Struct @struct = Primitives.Char;
            return Value.Create(@struct, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, ch));
        }
        else if (literalNode.Value == null)
        {
            Struct @struct = Primitives.Char;
            return Value.Create(@struct, LLVMValueRef.CreateConstNull(@struct.LLVMType));
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

            return Value.Create(builtType, builtVal);
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
        LLVMValueRef builtVal;

        if ((destType is PtrType destPtrType && destPtrType.Equals(Primitives.Void))
            || (right.Type is PtrType rPtrType && rPtrType.BaseType.Equals(Primitives.Void)))
        {
            builtVal = right.LLVMValue;
        }
        else
        {
            builtVal = destType is Int
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
        }
        
        return Value.Create(destType, builtVal);
    }

    public Value CompilePow(Value left, Value right)
    {
        Struct i16 = Primitives.Int16;
        Struct i32 = Primitives.Int32;
        Struct i64 = Primitives.Int64;
        Struct f32 = Primitives.Float32;
        Struct f64 = Primitives.Float64;

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
            left = Value.Create(val.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
                    ? f64
                    : f32,
                val);
        }
        else if (left.Type is Float
            && !left.Type.Equals(Primitives.Float64))
        {
            val = Builder.BuildFPCast(SafeLoad(left).LLVMValue, destType);
            left = Value.Create(val.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
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
            if (left.Type.Equals(Primitives.Float64))
            {
                if (right.Type.Equals(Primitives.Float64))
                {
                    val = Builder.BuildFPCast(right.LLVMValue, LLVMTypeRef.Double);
                    right = Value.Create(f64, val);
                }

                intrinsic = "llvm.pow.f64";
            }
            else
            {
                if (right.Type.Equals(Primitives.Float32))
                {
                    val = Builder.BuildFPCast(right.LLVMValue, LLVMTypeRef.Float);
                    right = Value.Create(f32, val);
                }

                intrinsic = "llvm.pow.f32";
            }
        }
        else if (right.Type is Int)
        {
            if (left.Type.Equals(Primitives.Float64))
            {
                if (!right.Type.Equals(Primitives.UInt16)
                    && !right.Type.Equals(Primitives.Int16))
                {
                    val = Builder.BuildIntCast(right.LLVMValue, LLVMTypeRef.Int16);
                    right = Value.Create(i16, val);
                }

                intrinsic = "llvm.powi.f64.i16";
            }
            else
            {
                if (right.Type.Equals(Primitives.UInt32)
                    || right.Type.Equals(Primitives.Int32))
                {
                    val = Builder.BuildIntCast(right.LLVMValue, LLVMTypeRef.Int32);
                    right = Value.Create(i32, val);
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
                ? Value.Create(i64,
                    Builder.BuildFPToSI(result.LLVMValue,
                        LLVMTypeRef.Int64))
                : Value.Create(i32,
                    Builder.BuildFPToSI(result.LLVMValue,
                        LLVMTypeRef.Int32));
        }

        return result;
    }

    public Value CompileAssignment(Scope scope, BinaryOperationNode binaryOp)
    {
        Value left = CompileExpression(scope, binaryOp.Left); //TODO: does not work with arrays

        if (left is not Pointer ptrAssigned)
        {
            throw new Exception($"Cannot assign to non-pointer: #{left.Type}({left.LLVMValue})");
        }
        
        Value right = SafeLoad(CompileExpression(scope, binaryOp.Right));
        return ptrAssigned.Store(this, right);
    }

    public Variable CompileLocal(Scope scope, LocalDefNode localDef)
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
        
        LLVMValueRef llvmVariable = Builder.BuildAlloca(type.LLVMType, localDef.Name);
        Variable ret = new Variable(localDef.Name, WrapAsRef(type), llvmVariable);
        scope.Variables.Add(localDef.Name, ret);

        if (value != null)
        {
            Builder.BuildStore(SafeLoad(value).LLVMValue, llvmVariable);
        }

        return ret;
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

                if (CurrentFunction.Name.Contains($".{Reserved.Init}:"))
                {
                    context = scope.Variables[Reserved.Self];
                }
                else
                {
                    context = Value.Create(new PtrType(CurrentFunction.OwnerStruct), CurrentFunction.LLVMValue.FirstParam);
                }

                refNode = refNode.Child;
            }
            else if (refNode is TypeRefNode typeRef)
            {
                Struct @struct = GetStruct(typeRef.Name);
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

                context = new Pointer(new RefType(resultType),
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

    public Pointer CompileVarRef(Value? context, Scope scope, RefNode refNode)
    {
        if (context != null)
        {
            context = SafeLoad(context);
            
            if (!(context.Type is PtrType ptrType && ptrType.BaseType is Struct structType)) //TODO: why are they not pointers?
            {
                throw new Exception($"Cannot do field access on value of type \"{context.Type}\".");
            }
            
            Field field = structType.GetField(refNode.Name, CurrentFunction.OwnerStruct);
            return new Pointer(WrapAsRef(field.Type),
                Builder.BuildStructGEP2(structType.LLVMType,
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
            else if (CurrentNamespace.TryGetGlobal(refNode.Name, out IGlobal globalVar))
            {
                return (Variable)globalVar;
            }
            else
            {
                throw new Exception($"Variable \"{refNode.Name}\" does not exist.");
            }
        }
    }

    public Value CompileFuncCall(Value? context, Scope scope, FuncCallNode funcCall, Struct? sourceStruct = null)
    {
        Function? func;
        var argTypes = new List<Type>();
        var args = new List<Value>();

        foreach (ExpressionNode arg in funcCall.Arguments)
        {
            Value val = CompileExpression(scope, arg);
            argTypes.Add(val.Type is RefType @ref ? @ref.BaseType : val.Type);
            args.Add(SafeLoad(val));
        }

        var sig = new Signature(funcCall.Name, argTypes);
        context = SafeLoad(context);

        if (scope.Variables.TryGetValue(funcCall.Name, out Variable funcVar) && funcVar.Type.BaseType is FuncType funcVarType)
        {
            func = new Function(funcVarType, SafeLoad(funcVar).LLVMValue, new Parameter[0]);
        }
        else if (context != null && context.Type is PtrType ptrType && ptrType.BaseType is Class @class)
        {
            var newArgTypes = new List<Type>
            {
                new PtrType(@class)
            };
            
            newArgTypes.AddRange(argTypes);
            argTypes = newArgTypes;
            sig = new Signature(sig.Name, argTypes);
            func = @class.GetMethod(sig, CurrentFunction.OwnerStruct);
            
            var newArgs = new List<Value>
            {
                context.GetAddr(this) //TODO: is this correct?
            };
            
            newArgs.AddRange(args);
            args = newArgs;
        }
        else if (context == null
            && sourceStruct != null
            && sourceStruct.TryGetFunction(sig, CurrentFunction.OwnerStruct, false, out func))
        {
            // Keep empty
        }
        else if (context == null)
        {
            func = GetFunction(sig);
        }
        else
        {
            throw new Exception($"Function \"{funcCall.Name}\" does not exist.");
        }

        return func.Call(this, args.ToArray());
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
        if (CurrentFunction != null
            && CurrentFunction.OwnerStruct != null
            && CurrentFunction.OwnerStruct.TryGetFunction(sig, CurrentFunction.OwnerStruct, true, out Function func))
        {
            return func;
        }
        else if (CurrentNamespace.TryGetFunction(sig, out func))
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
        if (value == null)
        {
            return null;
        }

        return value.SafeLoad(this);
    }

    public RefType WrapAsRef(Type type)
        => type is RefType @ref
            ? @ref
            : new RefType(type);

    public IReadOnlyList<object> CleanAttributeArgs(ExpressionNode[] args)
    {
        var result = new List<object>();

        foreach (var expr in args)
        {
            if (expr is LiteralNode litNode)
            {
                result.Add(litNode.Value);
            }
            else
            {
                throw new Exception("Cannot pass non-literal parameters to an attribute.");
            }
        }

        return result;
    }

    private Namespace InitGlobalNamespace()
    {
        var @namespace = new Namespace(null, "root");

        @namespace.Structs.Add(Reserved.Void, Primitives.Void);
        @namespace.Structs.Add(Reserved.Float16, Primitives.Float16);
        @namespace.Structs.Add(Reserved.Float32, Primitives.Float32);
        @namespace.Structs.Add(Reserved.Float64, Primitives.Float64);
        @namespace.Structs.Add(Reserved.Bool, Primitives.Bool);
        @namespace.Structs.Add(Reserved.Char, Primitives.Char);
        @namespace.Structs.Add(Reserved.UInt8, Primitives.UInt8);
        @namespace.Structs.Add(Reserved.UInt16, Primitives.UInt16);
        @namespace.Structs.Add(Reserved.UInt32, Primitives.UInt32);
        @namespace.Structs.Add(Reserved.UInt64, Primitives.UInt64);
        @namespace.Structs.Add(Reserved.Int8, Primitives.Int8);
        @namespace.Structs.Add(Reserved.Int16, Primitives.Int16);
        @namespace.Structs.Add(Reserved.Int32, Primitives.Int32);
        @namespace.Structs.Add(Reserved.Int64, Primitives.Int64);

        foreach (Struct @struct in @namespace.Structs.Values)
        {
            @struct.AddBuiltins(this);
        }

        return @namespace;
    }

    private void AddDefaultForeigns()
    {
        Dictionary<string, FuncType> entries = new Dictionary<string, FuncType>();

        {
            entries.Add(Reserved.Malloc,
                new FuncType(new PtrType(Primitives.Void),
                    new Type[1]
                    {
                        Primitives.UInt64
                        
                    },
                    false));
            entries.Add(Reserved.Realloc,
                new FuncType(new PtrType(Primitives.Void),
                    new Type[2]
                    {
                        new PtrType(Primitives.Void),
                        Primitives.UInt64
                    },
                    false));
            entries.Add(Reserved.Free,
                new FuncType(Primitives.Void,
                    new Type[1]
                    {
                        new PtrType(Primitives.Void)
                    },
                    false));
        }

        foreach (var kv in entries)
        {
            _foreigns.Add(kv.Key, kv.Value);
            Module.AddFunction(kv.Key, kv.Value.BaseType.LLVMType);
        }
    }

    private IntrinsicFunction CreateIntrinsic(string name)
    {
        Pow func = name switch
        {
            "llvm.powi.f32.i32" => new Pow(name, Module, Primitives.Float32, Primitives.Float32, Primitives.Int32),
            "llvm.powi.f64.i16" => new Pow(name, Module, Primitives.Float64, Primitives.Float64, Primitives.Int64),
            "llvm.pow.f32" => new Pow(name, Module, Primitives.Float32, Primitives.Float32, Primitives.Float32),
            "llvm.pow.f64" => new Pow(name, Module, Primitives.Float64, Primitives.Float64, Primitives.Float64),
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

    private unsafe LLVMModuleRef LoadLLVMModule(string path)
    {
        var buffer = GetMemoryBufferFromFile(path);
        var module = Context.GetBitcodeModule(buffer);
        LLVMSharp.Interop.LLVM.DisposeMemoryBuffer(buffer);
        return module;
    }
    
    private unsafe LLVMMemoryBufferRef GetMemoryBufferFromFile(string path)
    {
        var buffer = new byte[Encoding.ASCII.GetMaxByteCount(path.Length) + 1];
        var count = Encoding.UTF8.GetBytes(path, buffer);
        buffer[count] = 0;
        fixed (byte* ptr = buffer)
        {
            sbyte* message;
            LLVMOpaqueMemoryBuffer* memoryBuffer;
            if (LLVMSharp.Interop.LLVM.CreateMemoryBufferWithContentsOfFile((sbyte*)ptr, &memoryBuffer, &message) != 0)
                throw new Exception(new string(message));
            return memoryBuffer;
        }
    }
}
