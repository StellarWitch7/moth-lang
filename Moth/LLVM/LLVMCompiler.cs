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
    public LLVMBuilderRef Builder { get; set; }
    public LLVMPassManagerRef FunctionPassManager { get; }
    public Namespace GlobalNamespace { get; }
    public List<Struct> Types { get; } = new List<Struct>();
    public List<DefinedFunction> Functions { get; } = new List<DefinedFunction>();
    public List<IGlobal> Globals { get; } = new List<IGlobal>();
    public Func<string, IReadOnlyList<object>, IAttribute> MakeAttribute { get; }

    private readonly Logger _logger = new Logger("moth/compiler");
    private readonly Dictionary<string, IntrinsicFunction> _intrinsics = new Dictionary<string, IntrinsicFunction>();
    private readonly Dictionary<string, FuncType> _foreigns = new Dictionary<string, FuncType>();
    private Dictionary<string, Type> _anonTypes = new Dictionary<string, Type>();
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

            foreach (StructNode classNode in script.ClassNodes)
            {
                if (classNode is TemplateNode genericClass)
                {
                    PrepareTemplate(genericClass);
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

            foreach (StructNode classNode in script.ClassNodes)
            {
                if (classNode is not TemplateNode)
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

            foreach (StructNode @struct in script.ClassNodes)
            {
                if (@struct is not TemplateNode)
                {
                    CompileType(@struct);
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

            foreach (StructNode classNode in script.ClassNodes)
            {
                if (classNode is not TemplateNode)
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

    public void PrepareTemplate(TemplateNode templateNode)
    {
        var attributes = new Dictionary<string, IAttribute>();
        
        foreach (AttributeNode attribute in templateNode.Attributes)
        {
            attributes.Add(attribute.Name, MakeAttribute(attribute.Name, CleanAttributeArgs(attribute.Arguments.ToArray())));
        }

        if (attributes.TryGetValue(Reserved.TargetOS, out IAttribute targetOS) && !((TargetOSAttribute)targetOS).Targets.Contains(Utils.GetOS()))
        {
            return;
        }
        
        CurrentNamespace.Templates.Add(templateNode.Name,
            new Template(CurrentNamespace,
                templateNode.Name,
                templateNode.Privacy,
                templateNode.Scope,
                _imports,
                attributes,
                PrepareTemplateParameters(templateNode.Params)));
    }

    public TemplateParameter[] PrepareTemplateParameters(IReadOnlyList<TemplateParameterNode> @params)
    {
        var result = new List<TemplateParameter>();
        
        foreach (var param in @params)
        {
            result.Add(new TemplateParameter(param.Name, new TemplateParameterBound[0], false));
        }

        return result.ToArray();
    }

    public void BuildTemplate(Template template, StructNode structNode, Struct @struct, IReadOnlyList<ExpressionNode> args)
    {
        var oldBuilder = Builder;
        var oldNamespace = _currentNamespace;
        var oldImports = _imports;
        var oldAnonTypes = _anonTypes;
        var oldFunction = _currentFunction;

        var newAnonTypes = new Dictionary<string, Type>();
        
        for (var i = 0; i < template.Params.Length; i++)
        {
            var param = template.Params[i];
            var arg = args[i];

            if (!param.IsConst)
            {
                newAnonTypes.Add(param.Name, ResolveType((TypeRefNode)arg));
            }
        }

        Builder = Context.CreateBuilder();
        CurrentNamespace = template.Parent;
        _imports = template.Imports;
        _anonTypes = newAnonTypes;
        CurrentFunction = null;

        if (template.Contents == null)
        {
            throw new Exception($"Template \"{template.Name}\"has no contents.");
        }
        
        CompileType(structNode, @struct);
        
        foreach (FuncDefNode funcDefNode in structNode.Scope.Statements.OfType<FuncDefNode>())
        {
            DefineFunction(funcDefNode, @struct);
        }
        
        foreach (FuncDefNode funcDefNode in structNode.Scope.Statements.OfType<FuncDefNode>())
        {
            CompileFunction(funcDefNode, @struct);
        }

        Builder.Dispose();
        Builder = oldBuilder;
        CurrentNamespace = oldNamespace;
        _imports = oldImports;
        _anonTypes = oldAnonTypes;
        CurrentFunction = oldFunction;
    }

    public void DefineType(StructNode structNode)
    {
        Struct newStruct;
        
        if (structNode.Scope == null)
        {
            newStruct = new OpaqueStruct(this,
                CurrentNamespace,
                structNode.Name,
                structNode.Privacy);
        }
        else
        {
            newStruct = new Struct(CurrentNamespace,
                structNode.Name,
                Context.CreateNamedStruct(structNode.Name),
                structNode.Privacy);
        }
        
        CurrentNamespace.Structs.Add(structNode.Name, newStruct);
        Types.Add(newStruct.AddBuiltins(this));
    }

    public void CompileType(StructNode structNode, Struct @struct = null)
    {
        var llvmTypes = new List<LLVMTypeRef>();

        if (@struct == null)
        {
            @struct = GetStruct(structNode.Name);
        }

        if (@struct is OpaqueStruct)
        {
            return;
        }
        
        uint index = 0;

        foreach (FieldDefNode field in structNode.Scope.Statements.OfType<FieldDefNode>())
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
        OverloadList overloads;
        
        if (@struct != null)
        {
            if (funcDefNode.IsStatic)
            {
                @struct.StaticMethods.TryAdd(func.Name, new OverloadList(func.Name));
                @struct.StaticMethods[func.Name].Add(func);
            }
            else
            {
                @struct.Methods.TryAdd(func.Name, new OverloadList(func.Name));
                @struct.Methods[func.Name].Add(func);
            }
        }
        else
        {
            CurrentNamespace.Functions.TryAdd(func.Name, new OverloadList(func.Name));
            CurrentNamespace.Functions[func.Name].Add(func);
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
        OverloadList? overloads;
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

        if (funcDefNode.IsForeign
            || funcDefNode.ExecutionBlock == null)
        {
            return;
        }
        else if (@struct != null
            && !funcDefNode.IsStatic
            && @struct.Methods.TryGetValue(funcDefNode.Name, out overloads)
            && overloads.TryGet(paramTypes, out func))
        {
            // Keep empty
        }
        else if (@struct != null
            && funcDefNode.IsStatic
            && @struct.StaticMethods.TryGetValue(funcDefNode.Name, out overloads)
            && overloads.TryGet(paramTypes, out func))
        {
            // Keep empty
        }
        else if (CurrentNamespace.Functions.TryGetValue(funcDefNode.Name, out overloads)
            && overloads.TryGet(paramTypes, out func))
        {
            // Keep empty
        }
        else
        {
            throw new Exception($"Cannot compile function \"{funcDefNode.Name}\" as it is undefined.");
        }
        
        CurrentFunction = func;
        func.OpeningScope = new Scope(func.LLVMValue.AppendBasicBlock("entry"));
        Builder.PositionAtEnd(func.OpeningScope.LLVMBlock);

        if (funcDefNode.Name == Reserved.Init && func.IsStatic && func.Type is MethodType methodType)
        {
            if (func.Type.ReturnType is Struct retType)
            {
                if (retType != methodType.OwnerStruct)
                {
                    throw new Exception($"Init method does not return the same type as its owner class " +
                        $"(\"{methodType.OwnerStruct.Name}\").");
                }
            }

            var @new = methodType.OwnerStruct.Init(this);
            func.OpeningScope.Variables.Add(@new.Name, @new);

            if (!(@new.Type.BaseType is Struct structOfNew))
            {
                throw new Exception($"Critical failure in the init of class \"{methodType.OwnerStruct}\".");
            }
            
            foreach (Field field in structOfNew.Fields.Values)
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
                    new VarType(CurrentFunction.Type.ParameterTypes[param.ParamIndex]),
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

    public void DefineGlobal(FieldDefNode globalDef, Struct? @struct = null)
    {
        Type globalType = ResolveType(globalDef.TypeRef);
        LLVMValueRef globalVal = Module.AddGlobal(globalType.LLVMType, globalDef.Name);
        GlobalVariable global = new GlobalVariable(globalDef.Name, new VarType(globalType), globalVal, globalDef.Privacy);
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
                    Value expr = CompileExpression(scope, @return.ReturnValue).ImplicitConvertTo(this, CurrentFunction.Type.ReturnType);
                    Builder.BuildRet(expr.LLVMValue);
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
                Value condition = CompileExpression(scope, @while.Condition).ImplicitConvertTo(this, Primitives.Bool);
                Builder.BuildCondBr(condition.LLVMValue, then, @continue);
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
                Value condition = CompileExpression(scope, @if.Condition).ImplicitConvertTo(this, Primitives.Bool);
                LLVMBasicBlockRef then = CurrentFunction.LLVMValue.AppendBasicBlock("then");
                LLVMBasicBlockRef @else = CurrentFunction.LLVMValue.AppendBasicBlock("else");
                LLVMBasicBlockRef @continue = null;
                bool thenReturned = false;
                bool elseReturned = false;

                Builder.BuildCondBr(condition.LLVMValue, then, @else);

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

        if (typeRef is LocalTypeRefNode localTypeRef)
        {
            if (!_anonTypes.TryGetValue(localTypeRef.Name, out type))
            {
                throw new Exception($"No template loaded with type parameter \"{localTypeRef.Name}\".");
            }
        }
        else if (typeRef is TemplateTypeRefNode tmplTypeRef)
        {
            Template template = GetTemplate(tmplTypeRef.Name);
            type = template.Build(this, tmplTypeRef.Arguments);
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
            type = Array.ResolveType(this, elementType);
        }
        else
        {
            Struct @struct = GetStruct(typeRef.Name);
            type = @struct;
        }
        
        for (int i = 0; i < typeRef.PointerDepth; i++)
        {
            type = new PtrType(type);
        }

        if (typeRef.IsRef)
        {
            type = new RefType(type);
        }

        return type;
    }

    public Value CompileExpression(Scope scope, ExpressionNode expr)
    {
        if (expr is BinaryOperationNode binaryOp)
        {
            return binaryOp.Type == OperationType.Assignment
                ? CompileAssignment(scope, binaryOp)
                : CompileOperation(scope, binaryOp);
        }
        else if (expr is CastNode cast)
        {
            return CompileCast(scope, cast);
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
            Value condition = CompileExpression(scope, @if.Condition).ImplicitConvertTo(this, Primitives.Bool);
            LLVMBasicBlockRef then = CurrentFunction.LLVMValue.AppendBasicBlock("then");
            LLVMBasicBlockRef @else = CurrentFunction.LLVMValue.AppendBasicBlock("else");
            LLVMBasicBlockRef @continue = CurrentFunction.LLVMValue.AppendBasicBlock("continue");

            //then
            Builder.PositionAtEnd(then);
            Value thenVal = CompileExpression(scope, @if.Then);

            //else
            Builder.PositionAtEnd(@else);
            Value elseVal = CompileExpression(scope, @if.Else).ImplicitConvertTo(this, thenVal.Type);

            //prior
            Builder.PositionAtEnd(scope.LLVMBlock);
            LLVMValueRef result = Builder.BuildAlloca(thenVal.Type.LLVMType, "result");
            Builder.BuildCondBr(condition.LLVMValue, then, @else);

            //then
            Builder.PositionAtEnd(then);
            Builder.BuildStore(thenVal.LLVMValue, result);
            Builder.BuildBr(@continue);

            //else
            Builder.PositionAtEnd(@else);
            Builder.BuildStore(elseVal.LLVMValue, result);
            Builder.BuildBr(@continue);

            //continue
            Builder.PositionAtEnd(@continue);
            scope.LLVMBlock = @continue;

            return thenVal.Type.Equals(elseVal.Type)
                ? Value.Create(thenVal.Type, result)
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
        else if (expr is ThisNode @this)
        {
            if (CurrentFunction.OwnerStruct == null)
            {
                throw new Exception("Attempted self-instance reference in a global function.");
            }

            if (CurrentFunction.IsStatic)
            {
                if (CurrentFunction.Name == Reserved.Init)
                {
                    return scope.Variables[Reserved.Self];
                }
                else
                {
                    throw new Exception("Attempted self-instance reference in a static method.");
                }
            }
            else
            {
                return Value.Create(new PtrType(CurrentFunction.OwnerStruct), CurrentFunction.LLVMValue.FirstParam);
            }
        }
        else if (expr is IndexAccessNode indexAccess)
        {
            var toBeIndexed = CompileExpression(scope, indexAccess.ToBeIndexed);
            Type arrType = toBeIndexed.Type;

            if (toBeIndexed.Type is RefType varType)
            {
                arrType = varType.BaseType;
            }

            if (arrType is PtrType ptrType)
            {
                Type resultType = ptrType.BaseType;

                if (indexAccess.Params.Count != 1)
                {
                    throw new Exception("Standard pointer indexer must be given one parameter as index.");
                }
                    
                return new Pointer(new RefType(resultType),
                    Builder.BuildInBoundsGEP2(resultType.LLVMType,
                        toBeIndexed.LLVMValue,
                        new LLVMValueRef[]
                        {
                            CompileExpression(scope, indexAccess.Params[0]).ImplicitConvertTo(this, Primitives.UInt64).LLVMValue
                        }));
            }
            else if (arrType is Struct @struct)
            {
                return CompileFuncCall(scope, new FuncCallNode(Reserved.Indexer, indexAccess.Params, indexAccess.ToBeIndexed), CurrentFunction.OwnerStruct);
            }
            else
            {
                throw new Exception($"Cannot use an index access on value of type \"{toBeIndexed.Type}\".");
            }
        }
        else if (expr is InverseNode inverse)
        {
            var value = CompileExpression(scope, inverse.Value).ImplicitConvertTo(this, Primitives.Bool);
            
            return Value.Create(Primitives.Bool,
                Builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ,
                    value.LLVMValue,
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0)));
        }
        else if (expr is IncrementVarNode incrementVar)
        {
            var value = CompileExpression(scope, incrementVar.Value);

            if (value is not Pointer ptr)
            {
                throw new Exception(); //TODO
            }

            if (ptr.Type is not RefType || ptr.Type.BaseType is PtrType or FuncType)
            {
                throw new Exception(); //TODO
            }
            
            var valToAdd = LLVMValueRef.CreateConstInt(ptr.Type.BaseType.LLVMType, 1); //TODO: float compat?
            ptr.Store(this, Value.Create(ptr.Type.BaseType,
                Builder.BuildAdd(value.ImplicitConvertTo(this, ptr.Type.BaseType).LLVMValue, valToAdd)));
            return value;
        }
        else if (expr is DecrementVarNode decrementVar)
        {
            var value = CompileExpression(scope, decrementVar.Value);

            if (value is not Pointer ptr)
            {
                throw new Exception(); //TODO
            }

            if (ptr.Type is not RefType || ptr.Type.BaseType is PtrType or FuncType)
            {
                throw new Exception(); //TODO
            }
            
            var valToAdd = LLVMValueRef.CreateConstInt(ptr.Type.BaseType.LLVMType, 1); //TODO: float compat?
            ptr.Store(this, Value.Create(ptr.Type.BaseType,
                Builder.BuildSub(value.ImplicitConvertTo(this, ptr.Type.BaseType).LLVMValue, valToAdd)));
            return value;
        }
        else if (expr is RefOfNode addrofNode)
        {
            Value value = CompileExpression(scope, addrofNode.Value);
            return value.GetRef(this);
        }
        else if (expr is DeRefNode loadNode)
        {
            Value value = CompileExpression(scope, loadNode.Value);
            return value.DeRef(this);
        }
        else if (expr is FuncCallNode funcCall)
        {
            return CompileFuncCall(scope, funcCall, CurrentFunction.OwnerStruct);
        }
        else if (expr is RefNode @ref)
        {
            return CompileVarRef(scope, @ref);
        }
        else if (expr is SubExprNode subExpr)
        {
            return CompileExpression(scope, subExpr.Expression);
        }
        else
        {
            if (expr == null)
            {
                throw new Exception("Critical! Cannot compile null expression.");
            }
            
            throw new NotImplementedException();
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
            return AbstractInt.Create((long)i32);
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
            Struct @struct = Primitives.Null;
            return Value.Create(@struct, LLVMValueRef.CreateConstNull(@struct.LLVMType));
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public Value CompileOperation(Scope scope, BinaryOperationNode binaryOp)
    {
        var result = binaryOp.Type switch
        {
            OperationType.And => Value.Create(Primitives.Bool,
                Builder.BuildAnd(CompileExpression(scope, binaryOp.Left).ImplicitConvertTo(this, Primitives.Bool).LLVMValue,
                    CompileExpression(scope, binaryOp.Right).ImplicitConvertTo(this, Primitives.Bool).LLVMValue)),
            OperationType.Or => Value.Create(Primitives.Bool,
                Builder.BuildOr(CompileExpression(scope, binaryOp.Left).ImplicitConvertTo(this, Primitives.Bool).LLVMValue,
                    CompileExpression(scope, binaryOp.Right).ImplicitConvertTo(this, Primitives.Bool).LLVMValue)),
            OperationType.NotEqual => Value.Create(Primitives.Bool,
                Builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ,
                    CompileFuncCall(scope,
                        new FuncCallNode(Utils.ExpandOpName(Utils.OpTypeToString(binaryOp.Type)),
                            new ExpressionNode[]
                            {
                                binaryOp.Right
                            },
                            binaryOp.Left),
                        CurrentFunction.OwnerStruct).ImplicitConvertTo(this, Primitives.Bool).LLVMValue,
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0))),
            _ => CompileFuncCall(scope,
                new FuncCallNode(Utils.ExpandOpName(Utils.OpTypeToString(binaryOp.Type)),
                    new ExpressionNode[]
                    {
                        binaryOp.Right
                    },
                    binaryOp.Left),
                CurrentFunction.OwnerStruct)
        };

        if (binaryOp.Type == OperationType.Equal)
        {
            result = result.ImplicitConvertTo(this, Primitives.Bool);
        }
        
        return result;
    }

    public Value CompileCast(Scope scope, CastNode cast)
    {
        Value value = CompileExpression(scope, cast.Value);

        if (value.Type is VarType varType)
        {
            value = value.DeRef(this);
        }
        
        Type destType = ResolveType(cast.NewType);
        LLVMValueRef builtVal;

        if ((destType is PtrType destPtrType && destPtrType.Equals(Primitives.Void))
            || (value.Type is PtrType rPtrType && rPtrType.BaseType.Equals(Primitives.Void)))
        {
            builtVal = value.LLVMValue;
        }
        else
        {
            builtVal = destType is Int
                ? value.Type is Int
                    ? destType.Equals(Primitives.Bool)
                        ? Builder.BuildICmp(LLVMIntPredicate.LLVMIntNE,
                            LLVMValueRef.CreateConstInt(value.Type.LLVMType, 0), value.LLVMValue)
                        : value.Type.Equals(Primitives.Bool)
                            ? Builder.BuildZExt(value.LLVMValue, destType.LLVMType)
                            : Builder.BuildIntCast(value.LLVMValue, destType.LLVMType)
                    : value.Type is Float
                        ? destType is UnsignedInt
                            ? Builder.BuildFPToUI(value.LLVMValue, destType.LLVMType)
                            : destType is SignedInt
                                ? Builder.BuildFPToSI(value.LLVMValue, destType.LLVMType)
                                : throw new NotImplementedException()
                        : throw new NotImplementedException()
                : destType is Float
                    ? value.Type is Float
                        ? Builder.BuildFPCast(value.LLVMValue, destType.LLVMType)
                        : value.Type is Int
                            ? value.Type is UnsignedInt
                                ? Builder.BuildUIToFP(value.LLVMValue, destType.LLVMType)
                                : value.Type is SignedInt
                                    ? Builder.BuildSIToFP(value.LLVMValue, destType.LLVMType)
                                    : throw new NotImplementedException()
                            : throw new NotImplementedException()
                    : Builder.BuildCast(LLVMOpcode.LLVMBitCast,
                        value.LLVMValue,
                        destType.LLVMType);
        }
        
        return Value.Create(destType, builtVal);
    }

    // public Value CompilePow(Value left, Value right)
    // {
    //     Struct i16 = Primitives.Int16;
    //     Struct i32 = Primitives.Int32;
    //     Struct i64 = Primitives.Int64;
    //     Struct f32 = Primitives.Float32;
    //     Struct f64 = Primitives.Float64;
    //
    //     LLVMValueRef val;
    //     string intrinsic;
    //     LLVMTypeRef destType = LLVMTypeRef.Float;
    //     bool returnInt = left.Type is Int && right.Type is Int;
    //
    //     if (left.Type is Int)
    //     {
    //         if (left.Type.Equals(Primitives.UInt64)
    //             || left.Type.Equals(Primitives.Int64))
    //         {
    //             destType = LLVMTypeRef.Double;
    //         }
    //
    //         val = left.Type is SignedInt
    //             ? Builder.BuildSIToFP(left.LLVMValue, destType)
    //             : left.Type is UnsignedInt
    //                 ? Builder.BuildUIToFP(left.LLVMValue, destType)
    //                 : throw new NotImplementedException();
    //         left = Value.Create(val.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
    //                 ? f64
    //                 : f32,
    //             val);
    //     }
    //     else if (left.Type is Float
    //         && !left.Type.Equals(Primitives.Float64))
    //     {
    //         val = Builder.BuildFPCast(left.LLVMValue, destType);
    //         left = Value.Create(val.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
    //                 ? f64
    //                 : f32,
    //             val);
    //     }
    //     else
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     if (right.Type is Float)
    //     {
    //         if (left.Type.Equals(Primitives.Float64))
    //         {
    //             if (right.Type.Equals(Primitives.Float64))
    //             {
    //                 val = Builder.BuildFPCast(right.LLVMValue, LLVMTypeRef.Double);
    //                 right = Value.Create(f64, val);
    //             }
    //
    //             intrinsic = "llvm.pow.f64";
    //         }
    //         else
    //         {
    //             if (right.Type.Equals(Primitives.Float32))
    //             {
    //                 val = Builder.BuildFPCast(right.LLVMValue, LLVMTypeRef.Float);
    //                 right = Value.Create(f32, val);
    //             }
    //
    //             intrinsic = "llvm.pow.f32";
    //         }
    //     }
    //     else if (right.Type is Int)
    //     {
    //         if (left.Type.Equals(Primitives.Float64))
    //         {
    //             if (!right.Type.Equals(Primitives.UInt16)
    //                 && !right.Type.Equals(Primitives.Int16))
    //             {
    //                 val = Builder.BuildIntCast(right.LLVMValue, LLVMTypeRef.Int16);
    //                 right = Value.Create(i16, val);
    //             }
    //
    //             intrinsic = "llvm.powi.f64.i16";
    //         }
    //         else
    //         {
    //             if (right.Type.Equals(Primitives.UInt32)
    //                 || right.Type.Equals(Primitives.Int32))
    //             {
    //                 val = Builder.BuildIntCast(right.LLVMValue, LLVMTypeRef.Int32);
    //                 right = Value.Create(i32, val);
    //             }
    //
    //             intrinsic = "llvm.powi.f32.i32";
    //         }
    //     }
    //     else
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     IntrinsicFunction func = GetIntrinsic(intrinsic);
    //     var args = new Value[2]
    //     {
    //         left,
    //         right,
    //     };
    //     Value result = func.Call(this, args);
    //
    //     if (returnInt)
    //     {
    //         result = result.LLVMValue.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
    //             ? Value.Create(i64,
    //                 Builder.BuildFPToSI(result.LLVMValue,
    //                     LLVMTypeRef.Int64))
    //             : Value.Create(i32,
    //                 Builder.BuildFPToSI(result.LLVMValue,
    //                     LLVMTypeRef.Int32));
    //     }
    //
    //     return result;
    // }

    public Value CompileAssignment(Scope scope, BinaryOperationNode binaryOp)
    {
        Value left = CompileExpression(scope, binaryOp.Left);

        if (left is not Pointer ptrAssigned)
        {
            throw new Exception($"Cannot assign to non-pointer: #{left.Type}({left.LLVMValue})");
        }
        
        Value right = CompileExpression(scope, binaryOp.Right).ImplicitConvertTo(this, ptrAssigned.Type.BaseType);
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
        Variable ret = new Variable(localDef.Name, new VarType(type), llvmVariable);
        scope.Variables.Add(localDef.Name, ret);

        if (value != null)
        {
            Builder.BuildStore(value.ImplicitConvertTo(this, type).LLVMValue, llvmVariable);
        }

        return ret;
    }
    
    public Pointer CompileVarRef(Scope scope, RefNode @ref)
    {
        if (@ref.Parent != null)
        {
            Struct @struct;
            
            var parent = CompileExpression(scope, @ref.Parent);
            
            if (parent.Type is Struct temporary)
            {
                var ptrToStruct = Value.CreatePtrToTemp(this, parent);
                parent = ptrToStruct;
                @struct = temporary;
            }
            else if (parent.Type is PtrType ptrType && ptrType.BaseType is Struct structType)
            {
                @struct = structType;
            }
            else
            {
                throw new Exception($"Cannot do field access on value of type \"{parent.Type}\".");
            }
            
            Field field = @struct.GetField(@ref.Name, CurrentFunction.OwnerStruct);
            return new Pointer(new VarType(field.Type),
                Builder.BuildStructGEP2(@struct.LLVMType,
                    parent.LLVMValue,
                    field.FieldIndex,
                    field.Name));
        }
        else
        {
            if (scope.Variables.TryGetValue(@ref.Name, out Variable @var))
            {
                return @var;
            }
            else if (CurrentNamespace.TryGetGlobal(@ref.Name, out IGlobal globalVar))
            {
                return (Variable)globalVar;
            }
            else
            {
                throw new Exception($"Variable \"{@ref.Name}\" does not exist.");
            }
        }
    }

    public Value CompileFuncCall(Scope scope, FuncCallNode funcCall, Struct? sourceStruct = null)
    {
        Function? func;
        var argTypes = new List<Type>();
        var args = new List<Value>();

        foreach (ExpressionNode arg in funcCall.Arguments)
        {
            Value val = CompileExpression(scope, arg);

            if (val.Type is VarType varType)
            {
                val = val.DeRef(this);
            }
            
            argTypes.Add(val.Type);
            args.Add(val);
        }
        
        if (funcCall.ToCallOn != null)
        {
            if (funcCall.ToCallOn is TypeRefNode typeRef)
            {
                var type = ResolveType(typeRef);

                if (type is Struct @struct)
                {
                    if (@struct.StaticMethods.TryGetValue(funcCall.Name, out OverloadList overloads)
                        && overloads.TryGet(argTypes, out func))
                    {
                        // Keep empty
                    }
                    else
                    {
                        throw new Exception($"Static method \"{funcCall.Name}\" does not exist on struct \"{@struct}\".");
                    }
                }
                else
                {
                    throw new Exception($"Cannot call static method \"{funcCall.Name}\" on non-struct \"{type}\".");
                }
            }
            else
            {
                var toCallOn = CompileExpression(scope, funcCall.ToCallOn);
            
                PtrType ptrType;
                Struct @struct;

                if (toCallOn.Type is VarType)
                {
                    toCallOn = toCallOn.DeRef(this);
                }
                
                if (toCallOn.Type is Struct temporary)
                {
                    Pointer ptrToTemporary = Value.CreatePtrToTemp(this, toCallOn);
                    toCallOn = ptrToTemporary;
                    ptrType = ptrToTemporary.Type;
                    @struct = temporary;
                }
                else if (toCallOn.Type is PtrType structPtrType && structPtrType.BaseType is Struct baseType)
                {
                    ptrType = structPtrType;
                    @struct = baseType;
                }
                else
                {
                    throw new Exception("What");
                }
            
                var newArgTypes = new List<Type>
                {
                    ptrType
                };
            
                newArgTypes.AddRange(argTypes);
                argTypes = newArgTypes;
                func = @struct.GetMethod(funcCall.Name, argTypes, CurrentFunction.OwnerStruct);
            
                var newArgs = new List<Value>
                {
                    toCallOn
                };
            
                newArgs.AddRange(args);
                args = newArgs;
            }
        }
        else if (scope.Variables.TryGetValue(funcCall.Name, out Variable funcVar) && funcVar.Type.BaseType is FuncType funcVarType)
        {
            var funcVal = funcVar.DeRef(this);
            func = new Function(funcVarType, funcVal.LLVMValue, new Parameter[0]);
        }
        else if (sourceStruct != null
            && sourceStruct.TryGetFunction(funcCall.Name, argTypes, CurrentFunction.OwnerStruct, false, out func))
        {
            // Keep empty
        }
        else
        {
            func = GetFunction(funcCall.Name, argTypes);
        }

        int index = 0;
        
        foreach (var paramType in func.ParameterTypes)
        {
            args[index] = args[index].ImplicitConvertTo(this, paramType);
            index++;
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
    
    public Template GetTemplate(string name)
    {
        if (CurrentNamespace.TryGetTemplate(name, out Template template))
        {
            return template;
        }
        else if (_imports.TryGetTemplate(name, out template))
        {
            return template;
        }
        else
        {
            throw new Exception($"Could not find template \"{name}\".");
        }
    }

    public Function GetFunction(string name, IReadOnlyList<Type> paramTypes)
    {
        if (CurrentFunction != null
            && CurrentFunction.OwnerStruct != null
            && CurrentFunction.OwnerStruct.TryGetFunction(name, paramTypes, CurrentFunction.OwnerStruct, true, out Function func))
        {
            return func;
        }
        else if (CurrentNamespace.TryGetFunction(name, paramTypes, out func))
        {
            return func;
        }
        else if (_imports.TryGetFunction(name, paramTypes, out func))
        {
            return func;
        }
        else
        {
            throw new Exception($"Could not find function \"{name}\".");
        }
    }

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
