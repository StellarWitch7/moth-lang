using Moth.AST;
using Moth.AST.Node;
using Moth.LLVM.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using Array = Moth.LLVM.Data.Array;
using Pointer = Moth.LLVM.Data.Pointer;
using Type = Moth.LLVM.Data.Type;

namespace Moth.LLVM;

public class LLVMCompiler : IDisposable
{
    public string ModuleName { get; }
    public bool DoOptimize { get; }
    public LLVMContextRef Context { get; }
    public LLVMModuleRef Module { get; }
    public LLVMBuilderRef Builder { get; set; }
    public LLVMPassManagerRef FunctionPassManager { get; }
    public Namespace GlobalNamespace { get; }
    public List<Type> Types { get; } = new List<Type>();
    public List<Trait> Traits { get; } = new List<Trait>();
    public List<DefinedFunction> Functions { get; } = new List<DefinedFunction>();
    public List<IGlobal> Globals { get; } = new List<IGlobal>();
    public Func<string, IReadOnlyList<object>, IAttribute> MakeAttribute { get; }

    private readonly Logger _logger = new Logger("mothc/llvm");
    private readonly Dictionary<string, IntrinsicFunction> _intrinsics = new Dictionary<string, IntrinsicFunction>();
    private readonly Dictionary<string, FuncType> _foreigns = new Dictionary<string, FuncType>();
    private Dictionary<string, InternalType> _anonTypes = new Dictionary<string, InternalType>();
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
                _currentFunction = value.InternalType is FuncType
                    ? value
                    : throw new Exception("Cannot assign function value as it is not of a valid type. " +
                        "This is a CRITICAL ERROR. Report ASAP.");
            }
        }
    }
    
    public LLVMCompiler Compile(IReadOnlyCollection<ScriptAST> scripts)
    {
        foreach (ScriptAST script in scripts)
        {
            OpenFile(script.Namespace, script.Imports.ToArray());

            foreach (TypeNode typeNode in script.TypeNodes)
            {
                if (typeNode is TypeTemplateNode typeTemplateNode)
                {
                    PrepareTypeTemplate(typeTemplateNode);
                }
                else
                {
                    DefineType(typeNode);
                }
            }

            foreach (TraitNode traitNode in script.TraitNodes)
            {
                if (traitNode is TraitTemplateNode traitTemplateNode)
                {
                    PrepareTraitTemplate(traitTemplateNode);
                }
                else
                {
                    DefineTrait(traitNode);
                }
            }
        }

        foreach (ScriptAST script in scripts)
        {
            OpenFile(script.Namespace, script.Imports.ToArray());

            foreach (GlobalVarNode global in script.GlobalVariables)
            {
                DefineGlobal(global);
            }

            foreach (FuncDefNode funcDefNode in script.GlobalFunctions)
            {
                DefineFunction(funcDefNode);
            }
            
            foreach (TypeNode structNode in script.TypeNodes)
            {
                if (structNode is not TypeTemplateNode)
                {
                    var attributes = new Dictionary<string, IAttribute>();
        
                    foreach (AttributeNode attribute in structNode.Attributes)
                    {
                        attributes.Add(attribute.Name, MakeAttribute(attribute.Name, CleanAttributeArgs(attribute.Arguments.ToArray())));
                    }

                    if (!(attributes.TryGetValue(Reserved.TargetOS, out IAttribute targetOS) && !((TargetOSAttribute)targetOS).Targets.Contains(Utils.GetOS())))
                    {
                        Type type = GetType(structNode.Name);

                        if (structNode.Scope != null)
                        {
                            foreach (FuncDefNode funcDefNode in structNode.Scope.Statements.OfType<FuncDefNode>())
                            {
                                DefineFunction(funcDefNode, type);
                            }
                        }
                    }
                }
            }

            foreach (ImplementNode implementNode in script.ImplementNodes)
            {
                var trait = GetTrait(implementNode.Trait.Name); //TODO: traits should be treated like types, *mostly*

                if (ResolveType(implementNode.Type) is not Type type)
                    throw new Exception($"Cannot implement non-trait \"{implementNode.Type}\".");

                if (trait.IsExternal && type.IsExternal)
                    throw new Exception($"Cannot implement external trait \"{trait.FullName}\" for external type \"{type.FullName}\".");
                
                ImplementTraitForType(trait, type, implementNode.Implementations);
            }
        }

        foreach (ScriptAST script in scripts)
        {
            OpenFile(script.Namespace, script.Imports.ToArray());

            foreach (TypeNode @struct in script.TypeNodes)
            {
                if (@struct is not TypeTemplateNode)
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

            foreach (TypeNode structNode in script.TypeNodes)
            {
                if (structNode is not TypeTemplateNode)
                {
                    var attributes = new Dictionary<string, IAttribute>();
        
                    foreach (AttributeNode attribute in structNode.Attributes)
                    {
                        attributes.Add(attribute.Name, MakeAttribute(attribute.Name, CleanAttributeArgs(attribute.Arguments.ToArray())));
                    }

                    if (!(attributes.TryGetValue(Reserved.TargetOS, out IAttribute targetOS) && !((TargetOSAttribute)targetOS).Targets.Contains(Utils.GetOS())))
                    {
                        Type type = GetType(structNode.Name);

                        if (structNode.Scope != null)
                        {
                            foreach (FuncDefNode funcDefNode in structNode.Scope.Statements.OfType<FuncDefNode>())
                            {
                                CompileFunction(funcDefNode, type);
                            }
                        }
                    }
                }
            }
        }

        foreach (var type in Types)
        {
            if (type.TInfo == null && !type.IsExternal)
                Warn($"Type \"{type.FullName}\" has no registered TypeInfo constant.");
        }
        
        return this;
    }

    public IntrinsicFunction GetIntrinsic(string name)
        => _intrinsics.TryGetValue(name, out IntrinsicFunction? func)
            ? func
            : CreateIntrinsic(name);

    public Namespace ResolveNamespace(NamespaceNode nmspace) //TODO: improve
    {
        Namespace value = null;

        while (nmspace != null)
        {
            if (value != null)
            {
                if (value.Namespaces.TryGetValue(nmspace.Name, out Namespace o))
                {
                    value = o;
                }
                else
                {
                    var @new = new Namespace(value, nmspace.Name);
                    value.Namespaces.Add(nmspace.Name, @new);
                    value = @new;
                }
            }
            else
            {
                if (GlobalNamespace.Namespaces.TryGetValue(nmspace.Name, out Namespace o))
                {
                    value = o;
                }
                else
                {
                    var @new = new Namespace(GlobalNamespace, nmspace.Name);
                    GlobalNamespace.Namespaces.Add(nmspace.Name, @new);
                    value = @new;
                }
            }

            nmspace = nmspace.Child;
        }

        return value;
    }

    public Namespace[] ResolveImports(NamespaceNode[] imports)
    {
        List<Namespace> result = new List<Namespace>();

        foreach (var import in imports)
        {
            result.Add(ResolveNamespace(import));
        }

        return result.ToArray();
    }
    
    public void Warn(string message) => Log($"Warning: {message}");

    public void Log(string message) => _logger.WriteLine(message);
    
    public unsafe void LoadLibrary(string path)
    {
        using (var module = LoadLLVMModule(path))
        {
            var match = Regex.Match(Path.GetFileName(path),
                "(.*)(?=\\.mothlib.bc)");
        
            if (!match.Success)
            {
                throw new Exception($"Cannot load mothlibs, \"{path}\" does not have the correct extension.");
            }

            var libName = match.Value;
            var global = module.GetNamedGlobal($"<{libName}/metadata>");
            match = Regex.Match(global.ToString(), "(?<=<metadata>)(.*)(?=<\\/metadata>)");

            if (!match.Success)
            {
                throw new Exception($"Cannot load mothlibs, missing metadata for \"{libName}\".");
            }

            using (var metadata = new MemoryStream(Utils.Unescape(match.Value), false))
            {
                var deserializer = new MetadataDeserializer(this, metadata);
                deserializer.Process(libName);
            }
        }
    }

    public unsafe byte[] GenerateMetadata(string assemblyName)
    {
        using (var bytes = new MemoryStream())
        {
            MetadataSerializer serializer = new MetadataSerializer(this, bytes);
            bytes.Write(Encoding.UTF8.GetBytes("<metadata>"));
            serializer.Process();
            bytes.Write(Encoding.UTF8.GetBytes("</metadata>"));
            
            var global = Module.AddGlobal(LLVMTypeRef.CreateArray(LLVMTypeRef.Int8,
                    (uint)bytes.Length),
                $"<{assemblyName}/metadata>");
            
            global.Initializer = LLVMValueRef.CreateConstArray(LLVMTypeRef.Int8,
                bytes.ToArray().AsLLVMValues());
            global.Linkage = LLVMLinkage.LLVMDLLExportLinkage;
            global.IsGlobalConstant = true;
            
            return bytes.ToArray();
        }
    }

    public void PrepareTypeTemplate(TypeTemplateNode typeTemplateNode)
    {
        var attributes = new Dictionary<string, IAttribute>();
        
        foreach (AttributeNode attribute in typeTemplateNode.Attributes)
        {
            attributes.Add(attribute.Name, MakeAttribute(attribute.Name, CleanAttributeArgs(attribute.Arguments.ToArray())));
        }

        if (attributes.TryGetValue(Reserved.TargetOS, out IAttribute targetOS) && !((TargetOSAttribute)targetOS).Targets.Contains(Utils.GetOS()))
        {
            return;
        }
        
        CurrentNamespace.Templates.Add(typeTemplateNode.Name,
            new Template(this,
                CurrentNamespace,
                typeTemplateNode.Name,
                typeTemplateNode.Privacy,
                typeTemplateNode.Scope,
                _imports,
                typeTemplateNode.Attributes,
                PrepareTemplateParameters(typeTemplateNode.Params)));
    }

    public void PrepareTraitTemplate(TraitTemplateNode traitTemplateNode)
    {
        throw new NotImplementedException(); //TODO
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

    public void BuildTemplate(Template template, TypeNode typeNode, Type type, IReadOnlyList<ExpressionNode> args)
    {
        var oldBuilder = Builder;
        var oldNamespace = _currentNamespace;
        var oldImports = _imports;
        var oldAnonTypes = _anonTypes;
        var oldFunction = _currentFunction;

        var newAnonTypes = new Dictionary<string, InternalType>();
        
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
        
        CompileType(typeNode, type);
        
        foreach (FuncDefNode funcDefNode in typeNode.Scope.Statements.OfType<FuncDefNode>())
        {
            DefineFunction(funcDefNode, type);
        }
        
        foreach (FuncDefNode funcDefNode in typeNode.Scope.Statements.OfType<FuncDefNode>())
        {
            CompileFunction(funcDefNode, type);
        }

        Builder.Dispose();
        Builder = oldBuilder;
        CurrentNamespace = oldNamespace;
        _imports = oldImports;
        _anonTypes = oldAnonTypes;
        CurrentFunction = oldFunction;
    }

    public void DefineType(TypeNode typeNode)
    {
        Type newType;
        var attributes = new Dictionary<string, IAttribute>();
        
        foreach (AttributeNode attribute in typeNode.Attributes)
        {
            attributes.Add(attribute.Name, MakeAttribute(attribute.Name, CleanAttributeArgs(attribute.Arguments.ToArray())));
        }

        if (attributes.TryGetValue(Reserved.TargetOS, out IAttribute targetOS) && !((TargetOSAttribute)targetOS).Targets.Contains(Utils.GetOS()))
        {
            return;
        }
        
        if (typeNode.Scope == null)
        {
            newType = new OpaqueType(this,
                CurrentNamespace,
                typeNode.Name,
                attributes,
                typeNode.Privacy);
        }
        else
        {
            newType = new Type(this,
                CurrentNamespace,
                typeNode.Name,
                Context.CreateNamedStruct(typeNode.Name),
                attributes,
                typeNode.Privacy);
        }
        
        CurrentNamespace.Types.Add(typeNode.Name, newType);
        Types.Add(newType.AddBuiltins(this));
    }

    public void DefineTrait(TraitNode traitNode)
    {
        var attributes = new Dictionary<string, IAttribute>();
        
        foreach (AttributeNode attribute in traitNode.Attributes)
        {
            attributes.Add(attribute.Name, MakeAttribute(attribute.Name, CleanAttributeArgs(attribute.Arguments.ToArray())));
        }

        if (attributes.TryGetValue(Reserved.TargetOS, out IAttribute targetOS) && !((TargetOSAttribute)targetOS).Targets.Contains(Utils.GetOS()))
        {
            return;
        }

        var newTrait = new Trait(CurrentNamespace, traitNode.Name, attributes, traitNode.Privacy);
        CurrentNamespace.Traits.Add(traitNode.Name, newTrait);
        Traits.Add(newTrait);
    }

    public void CompileType(TypeNode typeNode, Type type = null)
    {
        var llvmTypes = new List<LLVMTypeRef>();
        var attributes = new Dictionary<string, IAttribute>();
        
        foreach (AttributeNode attribute in typeNode.Attributes)
        {
            attributes.Add(attribute.Name, MakeAttribute(attribute.Name, CleanAttributeArgs(attribute.Arguments.ToArray())));
        }

        if (attributes.TryGetValue(Reserved.TargetOS, out IAttribute targetOS) && !((TargetOSAttribute)targetOS).Targets.Contains(Utils.GetOS()))
        {
            return;
        }

        if (type == null)
        {
            type = GetType(typeNode.Name);
        }

        if (type is OpaqueType)
        {
            return;
        }
        
        uint index = 0;

        foreach (FieldDefNode field in typeNode.Scope.Statements.OfType<FieldDefNode>())
        {
            InternalType fieldType = ResolveType(field.TypeRef);
            llvmTypes.Add(fieldType.LLVMType);
            type.Fields.Add(field.Name, new Field(field.Name, index, fieldType, field.Privacy));
            index++;
        }

        type.LLVMType.StructSetBody(llvmTypes.AsReadonlySpan(), false);
    }

    public void DefineFunction(FuncDefNode funcDefNode, Type? @struct = null)
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
        var paramTypes = new List<InternalType>();

        if (@struct != null && !funcDefNode.IsStatic)
        {
            paramTypes.Add(new PtrType(@struct));
            index++;
        }

        foreach (ParameterNode paramNode in funcDefNode.Params)
        {
            InternalType paramType = ResolveParameter(paramNode);
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
        
        InternalType returnType = ResolveType(funcDefNode.ReturnTypeRef);
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

    public void CompileFunction(FuncDefNode funcDefNode, Type? @struct = null)
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
        var paramTypes = new List<InternalType>();

        foreach (ParameterNode param in funcDefNode.Params)
        {
            paramTypes.Add(ResolveParameter(param));
        }

        if (!funcDefNode.IsStatic && @struct != null)
        {
            var newParamTypes = new List<InternalType>()
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

        if (funcDefNode.Name == Reserved.Init && func.IsStatic && func.InternalType is MethodType methodType)
        {
            if (func.InternalType.ReturnType is Type retType)
            {
                if (retType != methodType.OwnerType)
                {
                    throw new Exception($"Init method does not return the same type as its owner class " +
                        $"(\"{methodType.OwnerType.Name}\").");
                }
            }

            var @new = methodType.OwnerType.Init(this);
            func.OpeningScope.Variables.Add(@new.Name, @new);

            if (!(@new.InternalType.BaseType is Type structOfNew))
            {
                throw new Exception($"Critical failure in the init of class \"{methodType.OwnerType}\".");
            }
            
            foreach (Field field in structOfNew.Fields.Values)
            {
                LLVMValueRef llvmField = Builder.BuildStructGEP2(methodType.OwnerType.LLVMType, @new.LLVMValue, field.FieldIndex);
                var zeroedVal = LLVMValueRef.CreateConstNull(field.InternalType.LLVMType);

                Builder.BuildStore(zeroedVal, llvmField);
            }
        }

        foreach (Parameter param in func.Params)
        {
            LLVMValueRef paramAsVar = Builder.BuildAlloca(func.InternalType.ParameterTypes[param.ParamIndex].LLVMType,
                param.Name);
            Builder.BuildStore(func.LLVMValue.Params[param.ParamIndex], paramAsVar);
            func.OpeningScope.Variables.Add(param.Name,
                new Variable(param.Name,
                    new VarType(CurrentFunction.InternalType.ParameterTypes[param.ParamIndex]),
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
    
    public InternalType ResolveParameter(ParameterNode param)
    {
        return ResolveType(param.TypeRef);
    }

    public void DefineGlobal(GlobalVarNode globalDef, Type? @struct = null)
    {
        var attributes = new Dictionary<string, IAttribute>();
        
        foreach (AttributeNode attribute in globalDef.Attributes)
        {
            attributes.Add(attribute.Name, MakeAttribute(attribute.Name, CleanAttributeArgs(attribute.Arguments.ToArray())));
        }

        if (attributes.TryGetValue(Reserved.TargetOS, out IAttribute targetOS) && !((TargetOSAttribute)targetOS).Targets.Contains(Utils.GetOS()))
        {
            return;
        }
        
        InternalType globalType = ResolveType(globalDef.TypeRef);
        LLVMValueRef globalVal = Module.AddGlobal(globalType.LLVMType, globalDef.Name);
        GlobalVariable global = new GlobalVariable(CurrentNamespace, globalDef.Name, new VarType(globalType), globalVal, attributes, globalDef.Privacy);
        globalVal.IsExternallyInitialized = globalDef.IsForeign;
        CurrentNamespace.GlobalVariables.Add(globalDef.Name, global);
        Globals.Add(global);
        //TODO: add const support to globals
    }

    public void ImplementTraitForType(Trait trait, Type type, ScopeNode implementations)
    {
        throw new NotImplementedException(); //TODO
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
                    Value expr = CompileExpression(scope, @return.ReturnValue);
                    expr = expr.ImplicitConvertTo(this, CurrentFunction.InternalType.ReturnType);
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

    public InternalType ResolveType(TypeRefNode typeRef)
    {
        InternalType internalType;

        if (typeRef is LocalTypeRefNode localTypeRef)
        {
            if (!_anonTypes.TryGetValue(localTypeRef.Name, out internalType))
            {
                throw new Exception($"No template loaded with type parameter \"{localTypeRef.Name}\".");
            }
        }
        else if (typeRef is TemplateTypeRefNode tmplTypeRef)
        {
            Template template = GetTemplate(tmplTypeRef.Name);
            internalType = template.Build(tmplTypeRef.Arguments);
        }
        else if (typeRef is FuncTypeRefNode fnTypeRef)
        {
            InternalType retType = ResolveType(fnTypeRef.ReturnType);
            var paramTypes = new List<InternalType>();

            foreach (TypeRefNode param in fnTypeRef.ParamterTypes)
            {
                InternalType paramType = ResolveType(param);
                paramTypes.Add(paramType);
            }

            return new LocalFuncType(retType, paramTypes.ToArray());
        }
        else if (typeRef is ArrayTypeRefNode arrayTypeRef)
        {
            InternalType elementType = ResolveType(arrayTypeRef.ElementType);
            internalType = Array.ResolveType(this, elementType);
        }
        else
        {
            Type type = GetType(typeRef.Name);
            internalType = type;
        }
        
        for (int i = 0; i < typeRef.PointerDepth; i++)
        {
            internalType = new PtrType(internalType);
        }

        if (typeRef.IsRef)
        {
            internalType = new RefType(internalType);
        }

        return internalType;
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
            InternalType retType = ResolveType(localFuncDef.ReturnTypeRef);
            var @params = new List<Parameter>();
            var paramTypes = new List<InternalType>();
            uint index = 0;

            foreach (ParameterNode param in localFuncDef.Params)
            {
                InternalType paramType = ResolveParameter(param);
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
            Value elseVal = CompileExpression(scope, @if.Else).ImplicitConvertTo(this, thenVal.InternalType);

            //prior
            Builder.PositionAtEnd(scope.LLVMBlock);
            LLVMValueRef result = Builder.BuildAlloca(thenVal.InternalType.LLVMType, "result");
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

            return thenVal.InternalType.Equals(elseVal.InternalType)
                ? Value.Create(thenVal.InternalType, result)
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
            InternalType arrType = toBeIndexed.InternalType;

            if (toBeIndexed.InternalType is RefType varType)
            {
                arrType = varType.BaseType;
            }

            if (arrType is PtrType ptrType)
            {
                InternalType resultType = ptrType.BaseType;

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
            else if (arrType is Type @struct)
            {
                return CompileFuncCall(scope, new FuncCallNode(Reserved.Indexer, indexAccess.Params, indexAccess.ToBeIndexed), CurrentFunction.OwnerStruct);
            }
            else
            {
                throw new Exception($"Cannot use an index access on value of type \"{toBeIndexed.InternalType}\".");
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

            if (ptr.InternalType is not RefType || ptr.InternalType.BaseType is PtrType or FuncType)
            {
                throw new Exception(); //TODO
            }
            
            var valToAdd = LLVMValueRef.CreateConstInt(ptr.InternalType.BaseType.LLVMType, 1); //TODO: float compat?
            ptr.Store(this, Value.Create(ptr.InternalType.BaseType,
                Builder.BuildAdd(value.ImplicitConvertTo(this, ptr.InternalType.BaseType).LLVMValue, valToAdd)));
            return value;
        }
        else if (expr is DecrementVarNode decrementVar)
        {
            var value = CompileExpression(scope, decrementVar.Value);

            if (value is not Pointer ptr)
            {
                throw new Exception(); //TODO
            }

            if (ptr.InternalType is not RefType || ptr.InternalType.BaseType is PtrType or FuncType)
            {
                throw new Exception(); //TODO
            }
            
            var valToAdd = LLVMValueRef.CreateConstInt(ptr.InternalType.BaseType.LLVMType, 1); //TODO: float compat?
            ptr.Store(this, Value.Create(ptr.InternalType.BaseType,
                Builder.BuildSub(value.ImplicitConvertTo(this, ptr.InternalType.BaseType).LLVMValue, valToAdd)));
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
            Type type = GetType(Reserved.UInt8);
            LLVMValueRef constStr = Context.GetConstString(str, false);
            LLVMValueRef global = Module.AddGlobal(constStr.TypeOf, "litstr");
            global.Initializer = constStr;
            global.Linkage = LLVMLinkage.LLVMPrivateLinkage;
            global.IsGlobalConstant = true;
            return Value.Create(new PtrType(type), global);
        }
        else if (literalNode.Value is bool @bool)
        {
            Type type = Primitives.Bool;
            return Value.Create(type, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, (ulong)(@bool ? 1 : 0)));
        }
        else if (literalNode.Value is int i32)
        {
            return AbstractInt.Create((long)i32);
        }
        else if (literalNode.Value is float f32)
        {
            Type type = Primitives.Float32;
            return Value.Create(type, LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, f32));
        }
        else if (literalNode.Value is char ch)
        {
            Type type = Primitives.UInt8;
            return Value.Create(type, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, ch));
        }
        else if (literalNode.Value == null)
        {
            Type type = Primitives.Null;
            return Value.Create(type, LLVMValueRef.CreateConstNull(type.LLVMType));
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
                        new FuncCallNode(Utils.ExpandOpName(Utils.OpTypeToString(OperationType.Equal)),
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

        if (value.InternalType is VarType varType)
        {
            value = value.DeRef(this);
        }
        
        InternalType destType = ResolveType(cast.NewType);
        LLVMValueRef builtVal;

        if (value.InternalType.CanConvertTo(destType))
        {
            builtVal = value.ImplicitConvertTo(this, destType).LLVMValue;
        }
        else
        {
            if (destType is Int)
            {
                if (value.InternalType is PtrType ptrTypeVal)
                {
                    if (destType.Equals(ptrTypeVal.BaseType))
                    {
                        builtVal = value.DeRef(this).LLVMValue;
                    }
                    else
                    {
                        builtVal = Builder.BuildPtrToInt(value.LLVMValue, destType.LLVMType);
                    }
                }
                else if (value.InternalType is Int)
                {
                    builtVal = destType.Equals(Primitives.Bool)
                        ? Builder.BuildICmp(LLVMIntPredicate.LLVMIntNE,
                            LLVMValueRef.CreateConstInt(value.InternalType.LLVMType, 0), value.LLVMValue)
                        : value.InternalType.Equals(Primitives.Bool)
                            ? Builder.BuildZExt(value.LLVMValue, destType.LLVMType)
                            : Builder.BuildIntCast(value.LLVMValue, destType.LLVMType);
                }
                else if (value.InternalType is Float)
                {
                    builtVal = destType is UnsignedInt
                        ? Builder.BuildFPToUI(value.LLVMValue, destType.LLVMType)
                        : destType is SignedInt
                            ? Builder.BuildFPToSI(value.LLVMValue, destType.LLVMType)
                            : throw new NotImplementedException();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if (destType is Float)
            {
                if (value.InternalType is Float)
                {
                    builtVal = Builder.BuildFPCast(value.LLVMValue, destType.LLVMType);
                }
                else if (value.InternalType is Int)
                {
                    builtVal = value.InternalType is UnsignedInt
                        ? Builder.BuildUIToFP(value.LLVMValue, destType.LLVMType)
                        : value.InternalType is SignedInt
                            ? Builder.BuildSIToFP(value.LLVMValue, destType.LLVMType)
                            : throw new NotImplementedException();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if (destType is PtrType ptrTypeDest)
            {
                if (value.InternalType.Equals(ptrTypeDest.BaseType))
                {
                    builtVal = value.GetRef(this).LLVMValue;
                }
                else if (value.InternalType is PtrType)
                {
                    builtVal = value.LLVMValue;
                }
                else if (value.InternalType is Int)
                {
                    builtVal = Builder.BuildIntToPtr(value.LLVMValue, ptrTypeDest.LLVMType);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                builtVal = Builder.BuildCast(LLVMOpcode.LLVMBitCast, value.LLVMValue, destType.LLVMType);
            }
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
            throw new Exception($"Cannot assign to non-pointer: #{left.InternalType}({left.LLVMValue})");
        }
        
        Value right = CompileExpression(scope, binaryOp.Right).ImplicitConvertTo(this, ptrAssigned.InternalType.BaseType);
        return ptrAssigned.Store(this, right);
    }

    public Variable CompileLocal(Scope scope, LocalDefNode localDef)
    {
        Value? value = null;
        InternalType type;

        if (localDef is InferredLocalDefNode inferredLocalDef)
        {
            value = CompileExpression(scope, inferredLocalDef.Value);
            type = value.InternalType;
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
            Type type;
            
            var parent = CompileExpression(scope, @ref.Parent);
            
            if (parent.InternalType is Type temporary)
            {
                var ptrToStruct = Value.CreatePtrToTemp(this, parent);
                parent = ptrToStruct;
                type = temporary;
            }
            else if (parent.InternalType is PtrType ptrType && ptrType.BaseType is Type structType)
            {
                type = structType;
            }
            else
            {
                throw new Exception($"Cannot do field access on value of type \"{parent.InternalType}\".");
            }
            
            Field field = type.GetField(@ref.Name, CurrentFunction.OwnerStruct);
            return new Pointer(new VarType(field.InternalType),
                Builder.BuildStructGEP2(type.LLVMType,
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

    public Value CompileFuncCall(Scope scope, FuncCallNode funcCall, Type? sourceStruct = null)
    {
        Function? func;
        Value? toCallOn = null;
        var argTypes = new List<InternalType>();
        var args = new List<Value>();

        foreach (ExpressionNode arg in funcCall.Arguments)
        {
            Value val = CompileExpression(scope, arg);

            if (val.InternalType is VarType varType)
            {
                val = val.DeRef(this);
            }
            
            argTypes.Add(val.InternalType);
            args.Add(val);
        }
        
        if (funcCall.ToCallOn != null)
        {
            if (funcCall.ToCallOn is TypeRefNode typeRef)
            {
                var type = ResolveType(typeRef);

                if (type is Type @struct)
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
                toCallOn = CompileExpression(scope, funcCall.ToCallOn);

                if (toCallOn.InternalType is VarType varType)
                {
                    if (varType.BaseType is Type)
                    {
                        toCallOn = Value.Create(new PtrType(@varType.BaseType), toCallOn.LLVMValue);
                    }
                    else
                    {
                        toCallOn = toCallOn.DeRef(this);
                    }
                }
                
                if (toCallOn.InternalType is Type temporary)
                {
                    Pointer ptrToTemporary = Value.CreatePtrToTemp(this, toCallOn);
                    var newArgTypes = new List<InternalType>
                    {
                        ptrToTemporary.InternalType
                    };
            
                    newArgTypes.AddRange(argTypes);
                    argTypes = newArgTypes;
                    toCallOn = ptrToTemporary;
                    func = temporary.GetMethod(funcCall.Name, argTypes, CurrentFunction.OwnerStruct);
                }
                else if (toCallOn.InternalType is AspectPtrType aspectPtrType)
                {
                    var newArgTypes = new List<InternalType>
                    {
                        aspectPtrType
                    };
            
                    newArgTypes.AddRange(argTypes);
                    argTypes = newArgTypes;
                    func = aspectPtrType.BaseType.GetMethod(funcCall.Name, argTypes);
                }
                else if (toCallOn.InternalType is PtrType structPtrType && structPtrType.BaseType is Type baseType)
                {
                    var newArgTypes = new List<InternalType>
                    {
                        structPtrType
                    };
            
                    newArgTypes.AddRange(argTypes);
                    argTypes = newArgTypes;
                    func = baseType.GetMethod(funcCall.Name, argTypes, CurrentFunction.OwnerStruct);
                }
                else
                {
                    throw new Exception("What"); //TODO
                }
            
                var newArgs = new List<Value>
                {
                    toCallOn
                };
            
                newArgs.AddRange(args);
                args = newArgs;
            }
        }
        else if (scope.Variables.TryGetValue(funcCall.Name, out Variable funcVar) && funcVar.InternalType.BaseType is FuncType funcVarType)
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

        if (func is AspectMethod aspMethod)
        {
            if (toCallOn == null || toCallOn is not AspectPointer aspPtr)
            {
                throw new Exception("How did you get that method from that source"); //TODO
            }

            return aspPtr.CallMethod(this, aspMethod, args.ToArray());
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

    public Type GetType(string name)
    {
        if (CurrentNamespace.TryGetType(name, out Type type))
        {
            return type;
        }
        else if (_imports.TryGetType(name, out type))
        {
            return type;
        }
        else
        {
            throw new Exception($"Could not find type \"{name}\".");
        }
    }
    
    public Trait GetTrait(string name)
    {
        if (CurrentNamespace.TryGetTrait(name, out Trait trait))
        {
            return trait;
        }
        else if (_imports.TryGetTrait(name, out trait))
        {
            return trait;
        }
        else
        {
            throw new Exception($"Could not find trait \"{name}\".");
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

    public Function GetFunction(string name, IReadOnlyList<InternalType> paramTypes)
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

    public void Dispose()
    {
        FunctionPassManager.Dispose();
        Builder.Dispose();
        Module.Dispose();
    }

    private Namespace InitGlobalNamespace()
    {
        var @namespace = new Namespace(null, "root");

        @namespace.Types.Add(Reserved.Void, Primitives.Void);
        @namespace.Types.Add(Reserved.Float16, Primitives.Float16);
        @namespace.Types.Add(Reserved.Float32, Primitives.Float32);
        @namespace.Types.Add(Reserved.Float64, Primitives.Float64);
        @namespace.Types.Add(Reserved.Bool, Primitives.Bool);
        @namespace.Types.Add(Reserved.UInt8, Primitives.UInt8);
        @namespace.Types.Add(Reserved.UInt16, Primitives.UInt16);
        @namespace.Types.Add(Reserved.UInt32, Primitives.UInt32);
        @namespace.Types.Add(Reserved.UInt64, Primitives.UInt64);
        @namespace.Types.Add(Reserved.Int8, Primitives.Int8);
        @namespace.Types.Add(Reserved.Int16, Primitives.Int16);
        @namespace.Types.Add(Reserved.Int32, Primitives.Int32);
        @namespace.Types.Add(Reserved.Int64, Primitives.Int64);

        foreach (Type @struct in @namespace.Types.Values)
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
                    new InternalType[1]
                    {
                        Primitives.UInt64
                        
                    },
                    false));
            entries.Add(Reserved.Realloc,
                new FuncType(new PtrType(Primitives.Void),
                    new InternalType[2]
                    {
                        new PtrType(Primitives.Void),
                        Primitives.UInt64
                    },
                    false));
            entries.Add(Reserved.Free,
                new FuncType(Primitives.Void,
                    new InternalType[1]
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
    
    private void OpenFile(NamespaceNode @namespace, NamespaceNode[] imports)
    {
        CurrentNamespace = ResolveNamespace(@namespace);
        _imports = ResolveImports(imports);
    }

    private unsafe LLVMModuleRef LoadLLVMModule(string path)
    {
        var buffer = GetMemoryBufferFromFile(path);
        var module = Context.GetBitcodeModule(buffer);
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
