using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using LLVMSharp;
using Moth.AST;
using Moth.AST.Node;
using Moth.LLVM.Data;
using Spectre.Console;

namespace Moth.LLVM;

public class LLVMCompiler : IDisposable
{
    public Null Null { get; }
    public Void Void { get; }

    public UnsignedInt Bool { get; }
    public UnsignedInt UInt8 { get; }
    public UnsignedInt UInt16 { get; }
    public UnsignedInt UInt32 { get; }
    public UnsignedInt UInt64 { get; }
    public UnsignedInt UInt128 { get; }

    public SignedInt Int8 { get; }
    public SignedInt Int16 { get; }
    public SignedInt Int32 { get; }
    public SignedInt Int64 { get; }
    public SignedInt Int128 { get; }

    public Float Float16 { get; }
    public Float Float32 { get; }
    public Float Float64 { get; }

    public string ModuleName { get; }
    public BuildOptions Options { get; }
    public LLVMContextRef Context { get; }
    public LLVMModuleRef Module { get; }
    public LLVMBuilderRef Builder { get; set; }
    public LLVMPassManagerRef FunctionPassManager { get; }
    public Namespace GlobalNamespace { get; }
    public HeaderBuilder Header { get; }
    public List<TypeDecl> Types { get; } = new List<TypeDecl>();
    public List<EnumDecl> Enums { get; } = new List<EnumDecl>();
    public List<TraitDecl> Traits { get; } = new List<TraitDecl>();
    public List<DefinedFunction> Functions { get; } = new List<DefinedFunction>();
    public List<IGlobal> Globals { get; } = new List<IGlobal>();
    public Func<string, IReadOnlyList<object>, IAttribute> MakeAttribute { get; }

    private readonly Logger _logger;
    private readonly Dictionary<string, IntrinsicFunction> _intrinsics =
        new Dictionary<string, IntrinsicFunction>();
    private readonly Dictionary<string, FuncType> _foreigns = new Dictionary<string, FuncType>();
    private Dictionary<string, Data.Type> _anonTypes = new Dictionary<string, Data.Type>();
    private Namespace[] _imports = null;
    private Namespace? _currentNamespace;
    private Function? _currentFunction;

    public LLVMCompiler(string moduleName, Logger parentLogger, BuildOptions options)
    {
        _logger = parentLogger.MakeSubLogger("llvm");
        ModuleName = moduleName;
        Options = options;
        Context = LLVMContextRef.Global;
        Builder = Context.CreateBuilder();
        Module = Context.CreateModuleWithName(ModuleName);
        Header = new HeaderBuilder(this);

        Null = new Null(this);
        Void = new Void(this);

        Bool = new UnsignedInt(this, Reserved.Bool, LLVMTypeRef.Int1, 1);
        UInt8 = new UnsignedInt(this, Reserved.UInt8, LLVMTypeRef.Int8, 8);
        UInt16 = new UnsignedInt(this, Reserved.UInt16, LLVMTypeRef.Int16, 16);
        UInt32 = new UnsignedInt(this, Reserved.UInt32, LLVMTypeRef.Int32, 32);
        UInt64 = new UnsignedInt(this, Reserved.UInt64, LLVMTypeRef.Int64, 64);
        UInt128 = new UnsignedInt(this, Reserved.UInt128, LLVMTypeRef.CreateInt(128), 128);

        Int8 = new SignedInt(this, Reserved.Int8, LLVMTypeRef.Int8, 8);
        Int16 = new SignedInt(this, Reserved.Int16, LLVMTypeRef.Int16, 16);
        Int32 = new SignedInt(this, Reserved.Int32, LLVMTypeRef.Int32, 32);
        Int64 = new SignedInt(this, Reserved.Int64, LLVMTypeRef.Int64, 64);
        Int128 = new SignedInt(this, Reserved.Int128, LLVMTypeRef.CreateInt(128), 128);

        Float16 = new Float(this, Reserved.Float16, LLVMTypeRef.Half, 16);
        Float32 = new Float(this, Reserved.Float32, LLVMTypeRef.Float, 32);
        Float64 = new Float(this, Reserved.Float64, LLVMTypeRef.Double, 64);

        GlobalNamespace = InitGlobalNamespace();
        AddDefaultForeigns();

        if (Options.DoOptimize)
        {
            Log("(unsafe) Creating function optimization pass manager...");

            unsafe
            {
                FunctionPassManager = LLVMSharp.Interop.LLVM.CreateFunctionPassManagerForModule(
                    Module
                );

                LLVMSharp.Interop.LLVM.AddInstructionCombiningPass(FunctionPassManager);
                LLVMSharp.Interop.LLVM.AddReassociatePass(FunctionPassManager);
                LLVMSharp.Interop.LLVM.AddGVNPass(FunctionPassManager);
                LLVMSharp.Interop.LLVM.AddCFGSimplificationPass(FunctionPassManager);

                LLVMSharp.Interop.LLVM.InitializeFunctionPassManager(FunctionPassManager);
            }
        }

        Log("Registering compiler attributes...");
        MakeAttribute = IAttribute.MakeCreationFunction(
            Assembly.GetExecutingAssembly().GetTypes().ToArray()
        );
    }

    public LLVMCompiler(
        string moduleName,
        BuildOptions options,
        Logger parentLogger,
        IReadOnlyCollection<ScriptAST> scripts
    )
        : this(moduleName, parentLogger, options) => Compile(scripts);

    public Version ModuleVersion
    {
        get => Options.Version;
    }

    public IContainer Parent
    {
        get => CurrentNamespace;
    }

    private Namespace CurrentNamespace
    {
        get { return _currentNamespace ?? throw new Exception("Current namespace is null."); }
        set { _currentNamespace = value; }
    }

    private Function CurrentFunction
    {
        get
        {
            return _currentFunction
                ?? throw new Exception(
                    "Current function is null. This is a CRITICAL ERROR. Report ASAP."
                );
        }
        set
        {
            if (value == null)
            {
                _currentFunction = null;
            }
            else
            {
                _currentFunction =
                    value.Type is FuncType
                        ? value
                        : throw new Exception(
                            "Cannot assign function value as it is not of a valid type. This is a CRITICAL ERROR. Report ASAP."
                        );
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

            foreach (EnumNode enumNode in script.EnumNodes)
            {
                if (enumNode is EnumTemplateNode enumTemplateNode)
                {
                    PrepareEnumTemplate(enumTemplateNode);
                }
                else
                {
                    DefineEnum(enumNode);
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
                        attributes.Add(
                            attribute.Name,
                            MakeAttribute(
                                attribute.Name,
                                CleanAttributeArgs(attribute.Arguments.ToArray())
                            )
                        );
                    }

                    if (
                        !(
                            attributes.TryGetValue(Reserved.TargetOS, out IAttribute targetOS)
                            && !((TargetOSAttribute)targetOS).Targets.Contains(Utils.GetOS())
                        )
                    )
                    {
                        TypeDecl typeDecl = GetType(structNode.Name);

                        if (structNode.Scope != null)
                        {
                            foreach (
                                FuncDefNode funcDefNode in structNode.Scope.Statements.OfType<FuncDefNode>()
                            )
                            {
                                DefineFunction(funcDefNode, typeDecl);
                            }
                        }
                    }
                }
            }

            foreach (ImplementNode implementNode in script.ImplementNodes)
            {
                var trait = GetTrait(implementNode.Trait.Name); //TODO: traits should be treated like types, *mostly*

                if (ResolveType(implementNode.Type) is not StructDecl type)
                    throw new Exception($"Cannot implement non-trait \"{implementNode.Type}\".");

                if (trait.IsExternal && type.IsExternal)
                    throw new Exception(
                        $"Cannot implement external trait \"{trait.FullName}\" for external type \"{type.FullName}\"."
                    );

                ImplementTraitForType(trait, type, implementNode.Implementations);
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
                        attributes.Add(
                            attribute.Name,
                            MakeAttribute(
                                attribute.Name,
                                CleanAttributeArgs(attribute.Arguments.ToArray())
                            )
                        );
                    }

                    if (
                        !(
                            attributes.TryGetValue(Reserved.TargetOS, out IAttribute targetOS)
                            && !((TargetOSAttribute)targetOS).Targets.Contains(Utils.GetOS())
                        )
                    )
                    {
                        TypeDecl typeDecl = GetType(structNode.Name);

                        if (structNode.Scope != null)
                        {
                            foreach (
                                FuncDefNode funcDefNode in structNode.Scope.Statements.OfType<FuncDefNode>()
                            )
                            {
                                CompileFunction(funcDefNode, typeDecl);
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

    public IntrinsicFunction GetIntrinsic(string name) =>
        _intrinsics.TryGetValue(name, out IntrinsicFunction? func) ? func : CreateIntrinsic(name);

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

    public void Log(string message) => _logger.Log(message);

    public void Info(string message) => _logger.Info(message);

    public void Warn(string message) => _logger.Warn(message);

    public void Error(string message) => _logger.Error(message);

    public unsafe void LoadLibrary(string path)
    {
        using (var module = LoadLLVMModule(path))
        {
            var match = Regex.Match(Path.GetFileName(path), "(.*)(?=\\.mothlib.bc)");

            if (!match.Success)
            {
                throw new Exception(
                    $"Cannot load mothlibs, \"{path}\" does not have the correct extension."
                );
            }

            var libName = match.Value;
            var global = module.GetNamedGlobal($"<{libName}/metadata>");
            match = Regex.Match(global.ToString(), "(?<=<metadata>)(.*)(?=<\\/metadata>)");

            if (!match.Success)
            {
                throw new Exception($"Cannot load mothlibs, missing metadata for \"{libName}\".");
            }

            using (
                var gzip = new GZipStream(
                    new MemoryStream(Utils.Unescape(match.Value), false),
                    CompressionMode.Decompress
                )
            )
            {
                using (var metadata = new MemoryStream())
                {
                    gzip.CopyTo(metadata);
                    metadata.Seek(0, SeekOrigin.Begin);

                    var deserializer = new MetadataDeserializer(this, metadata);
                    deserializer.Process(libName);
                }
            }
        }
    }

    public unsafe byte[] GenerateMetadata(string assemblyName)
    {
        using (var result = new MemoryStream())
        {
            result.Write(Encoding.UTF8.GetBytes("<metadata>"));

            using (var serializer = new MetadataSerializer(this))
            {
                var bytes = serializer.Process();
                var gzip = new GZipStream(result, Options.CompressionLevel);

                bytes.WriteTo(gzip);
                gzip.Flush();
            }

            result.Write(Encoding.UTF8.GetBytes("</metadata>"));

            var global = Module.AddGlobal(
                LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, (uint)result.Length),
                $"<{assemblyName}/metadata>"
            );

            global.Initializer = LLVMValueRef.CreateConstArray(
                LLVMTypeRef.Int8,
                result.ToArray().AsLLVMValues()
            );
            global.Linkage = LLVMLinkage.LLVMDLLExportLinkage;
            global.IsGlobalConstant = true;

            return result.ToArray();
        }
    }

    public void PrepareTypeTemplate(TypeTemplateNode typeTemplateNode)
    {
        var attributes = new Dictionary<string, IAttribute>();

        foreach (AttributeNode attribute in typeTemplateNode.Attributes)
        {
            attributes.Add(
                attribute.Name,
                MakeAttribute(attribute.Name, CleanAttributeArgs(attribute.Arguments.ToArray()))
            );
        }

        if (
            attributes.TryGetValue(Reserved.TargetOS, out IAttribute targetOS)
            && !((TargetOSAttribute)targetOS).Targets.Contains(Utils.GetOS())
        )
        {
            return;
        }

        CurrentNamespace.Templates.Add(
            typeTemplateNode.Name,
            new Template(
                this,
                CurrentNamespace,
                typeTemplateNode.Name,
                typeTemplateNode.Privacy,
                typeTemplateNode.IsUnion,
                typeTemplateNode.Scope,
                _imports,
                typeTemplateNode.Attributes,
                PrepareTemplateParameters(typeTemplateNode.Params)
            )
        );
    }

    public void PrepareTraitTemplate(TraitTemplateNode traitTemplateNode)
    {
        throw new NotImplementedException(); //TODO
    }

    public void PrepareEnumTemplate(EnumTemplateNode enumTemplateNode)
    {
        throw new NotImplementedException(); //TODO
    }

    public TemplateParameter[] PrepareTemplateParameters(
        IReadOnlyList<TemplateParameterNode> @params
    )
    {
        var result = new List<TemplateParameter>();

        foreach (var param in @params)
        {
            result.Add(new TemplateParameter(param.Name, new TemplateParameterBound[0], false));
        }

        return result.ToArray();
    }

    public void BuildTemplate(
        Template template,
        TypeNode typeNode,
        StructDecl structDecl,
        IReadOnlyList<ExpressionNode> args
    )
    {
        var oldBuilder = Builder;
        var oldNamespace = _currentNamespace;
        var oldImports = _imports;
        var oldAnonTypes = _anonTypes;
        var oldFunction = _currentFunction;

        var newAnonTypes = new Dictionary<string, Data.Type>();

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

        foreach (FuncDefNode funcDefNode in typeNode.Scope.Statements.OfType<FuncDefNode>())
        {
            DefineFunction(funcDefNode, structDecl);
        }

        foreach (FuncDefNode funcDefNode in typeNode.Scope.Statements.OfType<FuncDefNode>())
        {
            CompileFunction(funcDefNode, structDecl);
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
        StructDecl newStructDecl;
        var attributes = new Dictionary<string, IAttribute>();

        foreach (AttributeNode attribute in typeNode.Attributes)
        {
            attributes.Add(
                attribute.Name,
                MakeAttribute(attribute.Name, CleanAttributeArgs(attribute.Arguments.ToArray()))
            );
        }

        if (
            attributes.TryGetValue(Reserved.TargetOS, out IAttribute targetOS)
            && !((TargetOSAttribute)targetOS).Targets.Contains(Utils.GetOS())
        )
        {
            return;
        }

        if (typeNode.Scope == null)
        {
            newStructDecl = new OpaqueStructDecl(
                this,
                CurrentNamespace,
                typeNode.Name,
                typeNode.Privacy,
                typeNode.IsUnion,
                attributes
            );
        }
        else
        {
            newStructDecl = new StructDecl(
                this,
                CurrentNamespace,
                typeNode.Name,
                typeNode.Privacy,
                typeNode.IsUnion,
                attributes,
                typeNode.Scope
            );
        }

        CurrentNamespace.Types.Add(typeNode.Name, newStructDecl);
        Types.Add(newStructDecl.AddBuiltins());
    }

    public void DefineTrait(TraitNode traitNode)
    {
        var attributes = new Dictionary<string, IAttribute>();

        foreach (AttributeNode attribute in traitNode.Attributes)
        {
            attributes.Add(
                attribute.Name,
                MakeAttribute(attribute.Name, CleanAttributeArgs(attribute.Arguments.ToArray()))
            );
        }

        if (
            attributes.TryGetValue(Reserved.TargetOS, out IAttribute targetOS)
            && !((TargetOSAttribute)targetOS).Targets.Contains(Utils.GetOS())
        )
        {
            return;
        }

        var newTrait = new TraitDecl(
            this,
            CurrentNamespace,
            traitNode.Name,
            traitNode.Privacy,
            attributes
        );
        CurrentNamespace.Traits.Add(traitNode.Name, newTrait);
        Traits.Add(newTrait);
    }

    public void DefineEnum(EnumNode enumNode)
    {
        var attributes = new Dictionary<string, IAttribute>();

        foreach (AttributeNode attribute in enumNode.Attributes)
        {
            attributes.Add(
                attribute.Name,
                MakeAttribute(attribute.Name, CleanAttributeArgs(attribute.Arguments.ToArray()))
            );
        }

        if (
            attributes.TryGetValue(Reserved.TargetOS, out IAttribute targetOS)
            && !((TargetOSAttribute)targetOS).Targets.Contains(Utils.GetOS())
        )
        {
            return;
        }

        var newEnum = new EnumDecl(
            this,
            CurrentNamespace,
            enumNode.Name,
            enumNode.Privacy,
            attributes
        );

        foreach (var flag in enumNode.EnumFlags)
        {
            newEnum.Flags.Add(flag.Name, new EnumFlag(flag.Name, flag.Value));
        }

        CurrentNamespace.Types.Add(enumNode.Name, newEnum);
        Types.Add(newEnum);
    }

    public void DefineFunction(FuncDefNode funcDefNode, TypeDecl? typeDecl = null)
    {
        var attributes = new Dictionary<string, IAttribute>();

        foreach (AttributeNode attribute in funcDefNode.Attributes)
        {
            attributes.Add(
                attribute.Name,
                MakeAttribute(attribute.Name, CleanAttributeArgs(attribute.Arguments.ToArray()))
            );
        }

        if (attributes.TryGetValue(Reserved.TargetOS, out var attr))
        {
            if (!((TargetOSAttribute)attr).Targets.Contains(Utils.GetOS()))
                return;
        }

        uint index = 0;
        var @params = new List<Parameter>();
        var paramTypes = new List<Data.Type>();

        if (typeDecl != null && !funcDefNode.IsStatic)
        {
            paramTypes.Add(new PtrType(this, typeDecl));
            index++;
        }

        foreach (ParameterNode paramNode in funcDefNode.Params)
        {
            Data.Type paramType = ResolveParameter(paramNode);
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

        Data.Type returnType = ResolveType(funcDefNode.ReturnTypeRef);
        LLVMTypeRef llvmFuncType = LLVMTypeRef.CreateFunction(
            returnType.LLVMType,
            paramTypes.AsLLVMTypes().ToArray(),
            funcDefNode.IsVariadic
        );
        FuncType funcType =
            typeDecl == null
                ? new FuncType(this, returnType, paramTypes.ToArray(), funcDefNode.IsVariadic)
                : new MethodType(
                    this,
                    returnType,
                    paramTypes.ToArray(),
                    typeDecl,
                    funcDefNode.IsStatic
                );
        DefinedFunction func = new DefinedFunction(
            this,
            typeDecl == null ? CurrentNamespace : typeDecl,
            funcName,
            funcType,
            @params.ToArray(),
            funcDefNode.Privacy,
            funcDefNode.IsForeign,
            attributes
        );
        OverloadList overloads;

        if (typeDecl != null)
        {
            if (funcDefNode.IsStatic)
            {
                typeDecl.StaticMethods.TryAdd(func.Name, new OverloadList(func.Name));
                typeDecl.StaticMethods[func.Name].Add(func);
            }
            else
            {
                typeDecl.Methods.TryAdd(func.Name, new OverloadList(func.Name));
                typeDecl.Methods[func.Name].Add(func);
            }
        }
        else
        {
            CurrentNamespace.Functions.TryAdd(func.Name, new OverloadList(func.Name));
            CurrentNamespace.Functions[func.Name].Add(func);
        }

        Functions.Add(func);
    }

    public void CompileFunction(FuncDefNode funcDefNode, TypeDecl? typeDecl = null)
    {
        // Confirm that the definition is for the correct OS
        {
            foreach (AttributeNode attribute in funcDefNode.Attributes)
            {
                if (attribute.Name == Reserved.TargetOS)
                {
                    var targetOS = (TargetOSAttribute)MakeAttribute(
                        attribute.Name,
                        CleanAttributeArgs(attribute.Arguments.ToArray())
                    );
                    if (!targetOS.Targets.Contains(Utils.GetOS()))
                        return;
                }
            }
        }

        Function? func;
        OverloadList? overloads;
        var paramTypes = new List<Data.Type>();

        foreach (ParameterNode param in funcDefNode.Params)
        {
            paramTypes.Add(ResolveParameter(param));
        }

        if (!funcDefNode.IsStatic && typeDecl != null)
        {
            var newParamTypes = new List<Data.Type>() { new PtrType(this, typeDecl) };

            newParamTypes.AddRange(paramTypes);
            paramTypes = newParamTypes;
        }

        if (funcDefNode.IsForeign || funcDefNode.ExecutionBlock == null)
        {
            return;
        }
        else if (
            typeDecl != null
            && !funcDefNode.IsStatic
            && typeDecl.Methods.TryGetValue(funcDefNode.Name, out overloads)
            && overloads.TryGet(paramTypes, out func)
        )
        {
            // Keep empty
        }
        else if (
            typeDecl != null
            && funcDefNode.IsStatic
            && typeDecl.StaticMethods.TryGetValue(funcDefNode.Name, out overloads)
            && overloads.TryGet(paramTypes, out func)
        )
        {
            // Keep empty
        }
        else if (
            CurrentNamespace.Functions.TryGetValue(funcDefNode.Name, out overloads)
            && overloads.TryGet(paramTypes, out func)
        )
        {
            // Keep empty
        }
        else
        {
            throw new Exception(
                $"Cannot compile function \"{funcDefNode.Name}\" as it is undefined."
            );
        }

        CurrentFunction = func;
        func.OpeningScope = new Scope(func.LLVMValue.AppendBasicBlock("entry"));
        Builder.PositionAtEnd(func.OpeningScope.LLVMBlock);

        if (
            funcDefNode.Name == Reserved.Init
            && func.IsStatic
            && func.Type is MethodType methodType
        )
        {
            if (func.Type.ReturnType is not StructDecl retType)
            {
                throw new Exception("Init method must not be for a trait or enum.");
            }

            if (retType != methodType.OwnerTypeDecl)
            {
                throw new Exception(
                    $"Init method does not return the same type as its owner class (\"{methodType.OwnerTypeDecl.Name}\")."
                );
            }

            var @new = retType.Init();
            func.OpeningScope.Variables.Add(@new.Name, @new);

            if (!(@new.Type.BaseType is StructDecl structOfNew))
                throw new Exception(
                    $"Critical failure in the init of class \"{methodType.OwnerTypeDecl}\"."
                );

            foreach (Field field in structOfNew.Fields.Values)
            {
                LLVMValueRef llvmField = Builder.BuildStructGEP2(
                    methodType.OwnerTypeDecl.LLVMType,
                    @new.LLVMValue,
                    field.FieldIndex
                );
                var zeroedVal = LLVMValueRef.CreateConstNull(field.Type.LLVMType);

                Builder.BuildStore(zeroedVal, llvmField);
            }
        }

        foreach (Parameter param in func.Params)
        {
            LLVMValueRef paramAsVar = Builder.BuildAlloca(
                func.Type.ParameterTypes[param.ParamIndex].LLVMType,
                param.Name
            );
            Builder.BuildStore(func.LLVMValue.Params[param.ParamIndex], paramAsVar);
            func.OpeningScope.Variables.Add(
                param.Name,
                new Variable(
                    this,
                    param.Name,
                    new VarType(this, CurrentFunction.Type.ParameterTypes[param.ParamIndex]),
                    paramAsVar
                )
            );
        }

        if (CompileScope(func.OpeningScope, funcDefNode.ExecutionBlock))
        {
            if (Options.DoOptimize)
            {
                Log($"(unsafe) Running optimization pass on function \"{func.FullName}\".");

                unsafe
                {
                    LLVMSharp.Interop.LLVM.RunFunctionPassManager(
                        FunctionPassManager,
                        func.LLVMValue
                    );
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
                throw new Exception(
                    $"Foreign function \"{funcType}\" cannot overload other definition \"{prevType}\"."
                );
            }

            return Module.GetNamedFunction(funcName);
        }
        else
        {
            _foreigns.Add(funcName, funcType);
            return Module.AddFunction(funcName, funcType.BaseType.LLVMType);
        }
    }

    public Data.Type ResolveParameter(ParameterNode param)
    {
        return ResolveType(param.TypeRef);
    }

    public void DefineGlobal(GlobalVarNode globalDef, StructDecl? @struct = null)
    {
        var attributes = new Dictionary<string, IAttribute>();

        foreach (AttributeNode attribute in globalDef.Attributes)
        {
            attributes.Add(
                attribute.Name,
                MakeAttribute(attribute.Name, CleanAttributeArgs(attribute.Arguments.ToArray()))
            );
        }

        if (
            attributes.TryGetValue(Reserved.TargetOS, out IAttribute targetOS)
            && !((TargetOSAttribute)targetOS).Targets.Contains(Utils.GetOS())
        )
        {
            return;
        }

        Data.Type globalType = ResolveType(globalDef.TypeRef);
        LLVMValueRef globalVal = Module.AddGlobal(globalType.LLVMType, globalDef.Name);
        GlobalVariable global = new GlobalVariable(
            this,
            CurrentNamespace,
            globalDef.Name,
            new VarType(this, globalType),
            globalVal,
            attributes,
            globalDef.Privacy
        );
        globalVal.IsExternallyInitialized = globalDef.IsForeign;
        CurrentNamespace.GlobalVariables.Add(globalDef.Name, global);
        Globals.Add(global);
        //TODO: add const support to globals
    }

    public void ImplementTraitForType(
        TraitDecl traitDecl,
        StructDecl structDecl,
        ScopeNode implementations
    )
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
                    expr = expr.ImplicitConvertTo(CurrentFunction.Type.ReturnType);
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
                LLVMBasicBlockRef @continue = CurrentFunction.LLVMValue.AppendBasicBlock(
                    "continue"
                );
                Builder.BuildBr(loop);
                Builder.PositionAtEnd(loop);
                Value condition = CompileExpression(scope, @while.Condition)
                    .ImplicitConvertTo(Bool);
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
                Value condition = CompileExpression(scope, @if.Condition).ImplicitConvertTo(Bool);
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

    public Data.Type ResolveType(TypeRefNode typeRef)
    {
        Data.Type type;

        if (typeRef is LocalTypeRefNode localTypeRef)
        {
            if (!_anonTypes.TryGetValue(localTypeRef.Name, out type))
            {
                throw new Exception(
                    $"No template loaded with type parameter \"{localTypeRef.Name}\"."
                );
            }
        }
        else if (typeRef is TemplateTypeRefNode tmplTypeRef)
        {
            Template template = GetTemplate(tmplTypeRef.Name);
            type = template.Build(tmplTypeRef.Arguments);
        }
        else if (typeRef is FuncTypeRefNode fnTypeRef)
        {
            Data.Type retType = ResolveType(fnTypeRef.ReturnType);
            var paramTypes = new List<Data.Type>();

            foreach (TypeRefNode param in fnTypeRef.ParameterTypes)
            {
                Data.Type paramType = ResolveType(param);
                paramTypes.Add(paramType);
            }

            type = new LocalFuncType(this, retType, paramTypes.ToArray());
        }
        else if (typeRef is ArrayTypeRefNode arrayTypeRef)
        {
            Data.Type elementType = ResolveType(arrayTypeRef.ElementType);
            type = Array.ResolveType(this, elementType);
        }
        else
        {
            TypeDecl typeDecl = GetType(typeRef.Name);
            type = typeDecl;
        }

        for (int i = 0; i < typeRef.PointerDepth; i++)
        {
            type = new PtrType(this, type);
        }

        if (typeRef.IsRef)
        {
            type = new RefType(this, type);
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
            Data.Type retType = ResolveType(localFuncDef.ReturnTypeRef);
            var @params = new List<Parameter>();
            var paramTypes = new List<Data.Type>();
            uint index = 0;

            foreach (ParameterNode param in localFuncDef.Params)
            {
                Data.Type paramType = ResolveParameter(param);
                paramTypes.Add(paramType);
                @params.Add(new Parameter(index, param.Name));
                index++;
            }

            var funcType = new LocalFuncType(this, retType, paramTypes.ToArray());
            LLVMValueRef llvmFunc = Module.AddFunction(
                Reserved.LocalFunc,
                funcType.BaseType.LLVMType
            );

            Function? parentFunction = CurrentFunction;
            var func = new Function(this, funcType, llvmFunc, @params.ToArray())
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
            Value condition = CompileExpression(scope, @if.Condition).ImplicitConvertTo(Bool);
            LLVMBasicBlockRef then = CurrentFunction.LLVMValue.AppendBasicBlock("then");
            LLVMBasicBlockRef @else = CurrentFunction.LLVMValue.AppendBasicBlock("else");
            LLVMBasicBlockRef @continue = CurrentFunction.LLVMValue.AppendBasicBlock("continue");

            //then
            Builder.PositionAtEnd(then);
            Value thenVal = CompileExpression(scope, @if.Then);

            //else
            Builder.PositionAtEnd(@else);
            Value elseVal = CompileExpression(scope, @if.Else).ImplicitConvertTo(thenVal.Type);

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
                ? Value.Create(this, thenVal.Type, result)
                : throw new Exception(
                    "Then and else statements of inline if are not of the same type."
                );
        }
        else if (expr is LiteralArrayNode literalArrayNode)
        {
            return new Array(
                this,
                ResolveType(literalArrayNode.ElementType),
                literalArrayNode.Elements.CompileToValues(this, scope)
            );
        }
        else if (expr is LiteralNode literalNode)
        {
            return CompileLiteral(literalNode);
        }
        else if (expr is ThisNode @this)
        {
            if (CurrentFunction.OwnerType == null)
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
                return Value.Create(
                    this,
                    new PtrType(this, CurrentFunction.OwnerType),
                    CurrentFunction.LLVMValue.FirstParam
                );
            }
        }
        else if (expr is IndexAccessNode indexAccess)
        {
            var toBeIndexed = CompileExpression(scope, indexAccess.ToBeIndexed);
            Data.Type arrType = toBeIndexed.Type;

            if (toBeIndexed.Type is RefType varType)
            {
                arrType = varType.BaseType;
            }

            if (arrType is PtrType ptrType)
            {
                Data.Type resultType = ptrType.BaseType;

                if (indexAccess.Params.Count != 1)
                {
                    throw new Exception(
                        "Standard pointer indexer must be given one parameter as index."
                    );
                }

                return new Pointer(
                    this,
                    new RefType(this, resultType),
                    Builder.BuildInBoundsGEP2(
                        resultType.LLVMType,
                        toBeIndexed.LLVMValue,
                        new LLVMValueRef[]
                        {
                            CompileExpression(scope, indexAccess.Params[0])
                                .ImplicitConvertTo(UInt64)
                                .LLVMValue
                        }
                    )
                );
            }
            else if (arrType is StructDecl @struct)
            {
                return CompileFuncCall(
                    scope,
                    new FuncCallNode(Reserved.Indexer, indexAccess.Params, indexAccess.ToBeIndexed),
                    CurrentFunction.OwnerType
                );
            }
            else
            {
                throw new Exception(
                    $"Cannot use an index access on value of type \"{toBeIndexed.Type}\"."
                );
            }
        }
        else if (expr is InverseNode inverse)
        {
            var value = CompileExpression(scope, inverse.Value).ImplicitConvertTo(Bool);

            return Value.Create(
                this,
                Bool,
                Builder.BuildICmp(
                    LLVMIntPredicate.LLVMIntEQ,
                    value.LLVMValue,
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0)
                )
            );
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
            ptr.Store(
                Value.Create(
                    this,
                    ptr.Type.BaseType,
                    Builder.BuildAdd(value.ImplicitConvertTo(ptr.Type.BaseType).LLVMValue, valToAdd)
                )
            );
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
            ptr.Store(
                Value.Create(
                    this,
                    ptr.Type.BaseType,
                    Builder.BuildSub(value.ImplicitConvertTo(ptr.Type.BaseType).LLVMValue, valToAdd)
                )
            );
            return value;
        }
        else if (expr is RefOfNode addrofNode)
        {
            Value value = CompileExpression(scope, addrofNode.Value);
            return value.GetRef();
        }
        else if (expr is DeRefNode loadNode)
        {
            Value value = CompileExpression(scope, loadNode.Value);
            return value.DeRef();
        }
        else if (expr is FuncCallNode funcCall)
        {
            return CompileFuncCall(scope, funcCall, CurrentFunction.OwnerType);
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
            TypeDecl typeDecl = GetType(Reserved.UInt8);
            LLVMValueRef constStr = Context.GetConstString(str, false);
            LLVMValueRef global = Module.AddGlobal(constStr.TypeOf, "litstr");
            global.Initializer = constStr;
            global.Linkage = LLVMLinkage.LLVMPrivateLinkage;
            global.IsGlobalConstant = true;
            return Value.Create(this, new PtrType(this, typeDecl), global);
        }
        else if (literalNode.Value is bool @bool)
        {
            TypeDecl typeDecl = Bool;
            return Value.Create(
                this,
                typeDecl,
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, (ulong)(@bool ? 1 : 0))
            );
        }
        else if (literalNode.Value is int i32)
        {
            return AbstractInt.Create(this, (long)i32);
        }
        else if (literalNode.Value is float f32)
        {
            TypeDecl typeDecl = Float32;
            return Value.Create(
                this,
                typeDecl,
                LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, f32)
            );
        }
        else if (literalNode.Value is char ch)
        {
            TypeDecl typeDecl = UInt8;
            return Value.Create(this, typeDecl, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, ch));
        }
        else if (literalNode.Value == null)
        {
            TypeDecl typeDecl = Null;
            return Value.Create(this, typeDecl, LLVMValueRef.CreateConstNull(typeDecl.LLVMType));
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
            OperationType.And
                => Value.Create(
                    this,
                    Bool,
                    Builder.BuildAnd(
                        CompileExpression(scope, binaryOp.Left).ImplicitConvertTo(Bool).LLVMValue,
                        CompileExpression(scope, binaryOp.Right).ImplicitConvertTo(Bool).LLVMValue
                    )
                ),
            OperationType.Or
                => Value.Create(
                    this,
                    Bool,
                    Builder.BuildOr(
                        CompileExpression(scope, binaryOp.Left).ImplicitConvertTo(Bool).LLVMValue,
                        CompileExpression(scope, binaryOp.Right).ImplicitConvertTo(Bool).LLVMValue
                    )
                ),
            OperationType.NotEqual
                => Value.Create(
                    this,
                    Bool,
                    Builder.BuildICmp(
                        LLVMIntPredicate.LLVMIntEQ,
                        CompileFuncCall(
                                scope,
                                new FuncCallNode(
                                    Utils.ExpandOpName(Utils.OpTypeToString(OperationType.Equal)),
                                    new ExpressionNode[] { binaryOp.Right },
                                    binaryOp.Left
                                ),
                                CurrentFunction.OwnerType
                            )
                            .ImplicitConvertTo(Bool)
                            .LLVMValue,
                        LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0)
                    )
                ),
            _
                => CompileFuncCall(
                    scope,
                    new FuncCallNode(
                        Utils.ExpandOpName(Utils.OpTypeToString(binaryOp.Type)),
                        new ExpressionNode[] { binaryOp.Right },
                        binaryOp.Left
                    ),
                    CurrentFunction.OwnerType
                )
        };

        if (binaryOp.Type == OperationType.Equal)
        {
            result = result.ImplicitConvertTo(Bool);
        }

        return result;
    }

    public Value CompileCast(Scope scope, CastNode cast)
    {
        Value value = CompileExpression(scope, cast.Value);

        if (value.Type is VarType varType)
        {
            value = value.DeRef();
        }

        Data.Type destType = ResolveType(cast.NewType);
        LLVMValueRef builtVal;

        if (value.Type.CanConvertTo(destType))
        {
            builtVal = value.ImplicitConvertTo(destType).LLVMValue;
        }
        else
        {
            if (destType is Int)
            {
                if (value.Type is PtrType ptrTypeVal)
                {
                    if (destType.Equals(ptrTypeVal.BaseType))
                    {
                        builtVal = value.DeRef().LLVMValue;
                    }
                    else
                    {
                        builtVal = Builder.BuildPtrToInt(value.LLVMValue, destType.LLVMType);
                    }
                }
                else if (value.Type is Int)
                {
                    builtVal = destType.Equals(Bool)
                        ? Builder.BuildICmp(
                            LLVMIntPredicate.LLVMIntNE,
                            LLVMValueRef.CreateConstInt(value.Type.LLVMType, 0),
                            value.LLVMValue
                        )
                        : value.Type.Equals(Bool)
                            ? Builder.BuildZExt(value.LLVMValue, destType.LLVMType)
                            : Builder.BuildIntCast(value.LLVMValue, destType.LLVMType);
                }
                else if (value.Type is Float)
                {
                    builtVal =
                        destType is UnsignedInt
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
                if (value.Type is Float)
                {
                    builtVal = Builder.BuildFPCast(value.LLVMValue, destType.LLVMType);
                }
                else if (value.Type is Int)
                {
                    builtVal =
                        value.Type is UnsignedInt
                            ? Builder.BuildUIToFP(value.LLVMValue, destType.LLVMType)
                            : value.Type is SignedInt
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
                if (value.Type.Equals(ptrTypeDest.BaseType))
                {
                    builtVal = value.GetRef().LLVMValue;
                }
                else if (value.Type is PtrType)
                {
                    builtVal = value.LLVMValue;
                }
                else if (value.Type is Int)
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
                builtVal = Builder.BuildCast(
                    LLVMOpcode.LLVMBitCast,
                    value.LLVMValue,
                    destType.LLVMType
                );
            }
        }

        return Value.Create(this, destType, builtVal);
    }

    // public Value CompilePow(Value left, Value right)
    // {
    //     Struct i16 = Int16;
    //     Struct i32 = Int32;
    //     Struct i64 = Int64;
    //     Struct f32 = Float32;
    //     Struct f64 = Float64;
    //
    //     LLVMValueRef val;
    //     string intrinsic;
    //     LLVMTypeRef destType = LLVMTypeRef.Float;
    //     bool returnInt = left.Type is Int && right.Type is Int;
    //
    //     if (left.Type is Int)
    //     {
    //         if (left.Type.Equals(UInt64)
    //             || left.Type.Equals(Int64))
    //         {
    //             destType = LLVMTypeRef.Double;
    //         }
    //
    //         val = left.Type is SignedInt
    //             ? Builder.BuildSIToFP(left.LLVMValue, destType)
    //             : left.Type is UnsignedInt
    //                 ? Builder.BuildUIToFP(left.LLVMValue, destType)
    //                 : throw new NotImplementedException();
    //         left = Value.Create(this, val.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
    //                 ? f64
    //                 : f32,
    //             val);
    //     }
    //     else if (left.Type is Float
    //         && !left.Type.Equals(Float64))
    //     {
    //         val = Builder.BuildFPCast(left.LLVMValue, destType);
    //         left = Value.Create(this, val.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind
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
    //         if (left.Type.Equals(Float64))
    //         {
    //             if (right.Type.Equals(Float64))
    //             {
    //                 val = Builder.BuildFPCast(right.LLVMValue, LLVMTypeRef.Double);
    //                 right = Value.Create(this, f64, val);
    //             }
    //
    //             intrinsic = "llvm.pow.f64";
    //         }
    //         else
    //         {
    //             if (right.Type.Equals(Float32))
    //             {
    //                 val = Builder.BuildFPCast(right.LLVMValue, LLVMTypeRef.Float);
    //                 right = Value.Create(this, f32, val);
    //             }
    //
    //             intrinsic = "llvm.pow.f32";
    //         }
    //     }
    //     else if (right.Type is Int)
    //     {
    //         if (left.Type.Equals(Float64))
    //         {
    //             if (!right.Type.Equals(UInt16)
    //                 && !right.Type.Equals(Int16))
    //             {
    //                 val = Builder.BuildIntCast(right.LLVMValue, LLVMTypeRef.Int16);
    //                 right = Value.Create(this, i16, val);
    //             }
    //
    //             intrinsic = "llvm.powi.f64.i16";
    //         }
    //         else
    //         {
    //             if (right.Type.Equals(UInt32)
    //                 || right.Type.Equals(Int32))
    //             {
    //                 val = Builder.BuildIntCast(right.LLVMValue, LLVMTypeRef.Int32);
    //                 right = Value.Create(this, i32, val);
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
    //             ? Value.Create(this, i64,
    //                 Builder.BuildFPToSI(result.LLVMValue,
    //                     LLVMTypeRef.Int64))
    //             : Value.Create(this, i32,
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

        Value right = CompileExpression(scope, binaryOp.Right)
            .ImplicitConvertTo(ptrAssigned.Type.BaseType);
        return ptrAssigned.Store(right);
    }

    public Variable CompileLocal(Scope scope, LocalDefNode localDef)
    {
        Value? value = null;
        Data.Type type;

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
        Variable ret = new Variable(this, localDef.Name, new VarType(this, type), llvmVariable);
        scope.Variables.Add(localDef.Name, ret);

        if (value != null)
        {
            Builder.BuildStore(value.ImplicitConvertTo(type).LLVMValue, llvmVariable);
        }

        return ret;
    }

    public Value CompileVarRef(Scope scope, RefNode @ref)
    {
        if (@ref.Parent != null)
        {
            if (@ref.Parent is TypeRefNode staticParent)
            {
                TypeDecl typeDecl;

                var parent = ResolveType(staticParent);

                if (parent is StructDecl structDecl)
                {
                    throw new NotImplementedException();
                }
                else if (parent is EnumDecl enumDecl)
                {
                    return enumDecl.MakeValue(@ref.Name);
                }
                else
                {
                    throw new NotImplementedException(); //TODO: better error message
                }
            }
            else
            {
                StructDecl structDecl;

                var parent = CompileExpression(scope, @ref.Parent);

                if (parent.Type is StructDecl temporary)
                {
                    var ptrToStruct = Value.CreatePtrToTemp(this, parent);
                    parent = ptrToStruct;
                    structDecl = temporary;
                }
                else if (
                    parent.Type is PtrType ptrType
                    && ptrType.BaseType is StructDecl structType
                )
                {
                    structDecl = structType;
                }
                else
                {
                    throw new Exception(
                        $"Cannot do field access on value of type \"{parent.Type}\"."
                    );
                }

                Field field = structDecl.GetField(@ref.Name, CurrentFunction.OwnerType);
                return field.GetValue(parent);
            }
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

    public Value CompileFuncCall(Scope scope, FuncCallNode funcCall, TypeDecl? sourceType = null)
    {
        Function? func;
        Value? toCallOn = null;
        var argTypes = new List<Data.Type>();
        var args = new List<Value>();

        foreach (ExpressionNode arg in funcCall.Arguments)
        {
            Value val = CompileExpression(scope, arg);

            if (val.Type is VarType varType)
            {
                val = val.DeRef();
            }

            argTypes.Add(val.Type);
            args.Add(val);
        }

        if (funcCall.ToCallOn != null)
        {
            if (funcCall.ToCallOn is TypeRefNode typeRef)
            {
                var type = ResolveType(typeRef);

                if (type is StructDecl @struct)
                {
                    if (
                        @struct.StaticMethods.TryGetValue(funcCall.Name, out OverloadList overloads)
                        && overloads.TryGet(argTypes, out func)
                    )
                    {
                        // Keep empty
                    }
                    else
                    {
                        throw new Exception(
                            $"Static method \"{funcCall.Name}\" does not exist on struct \"{@struct}\"."
                        );
                    }
                }
                else
                {
                    throw new Exception(
                        $"Cannot call static method \"{funcCall.Name}\" on non-struct \"{type}\"."
                    );
                }
            }
            else
            {
                toCallOn = CompileExpression(scope, funcCall.ToCallOn);

                if (toCallOn.Type is VarType varType)
                {
                    if (varType.BaseType is StructDecl)
                    {
                        toCallOn = Value.Create(
                            this,
                            new PtrType(this, @varType.BaseType),
                            toCallOn.LLVMValue
                        );
                    }
                    else
                    {
                        toCallOn = toCallOn.DeRef();
                    }
                }

                if (toCallOn.Type is StructDecl temporary)
                {
                    Pointer ptrToTemporary = Value.CreatePtrToTemp(this, toCallOn);
                    var newArgTypes = new List<Data.Type> { ptrToTemporary.Type };

                    newArgTypes.AddRange(argTypes);
                    argTypes = newArgTypes;
                    toCallOn = ptrToTemporary;
                    func = temporary.GetMethod(funcCall.Name, argTypes, CurrentFunction.OwnerType);
                }
                else if (toCallOn.Type is TraitPtrType aspectPtrType)
                {
                    var newArgTypes = new List<Data.Type> { aspectPtrType };

                    newArgTypes.AddRange(argTypes);
                    argTypes = newArgTypes;
                    func = aspectPtrType.BaseType.GetMethod(funcCall.Name, argTypes);
                }
                else if (
                    toCallOn.Type is PtrType structPtrType
                    && structPtrType.BaseType is StructDecl baseType
                )
                {
                    var newArgTypes = new List<Data.Type> { structPtrType };

                    newArgTypes.AddRange(argTypes);
                    argTypes = newArgTypes;
                    func = baseType.GetMethod(funcCall.Name, argTypes, CurrentFunction.OwnerType);
                }
                else
                {
                    throw new Exception("What"); //TODO
                }

                var newArgs = new List<Value> { toCallOn };

                newArgs.AddRange(args);
                args = newArgs;
            }
        }
        else if (
            scope.Variables.TryGetValue(funcCall.Name, out Variable funcVar)
            && funcVar.Type.BaseType is FuncType funcVarType
        )
        {
            var funcVal = funcVar.DeRef();
            func = new Function(this, funcVarType, funcVal.LLVMValue, new Parameter[0]);
        }
        else if (
            sourceType != null
            && sourceType.TryGetFunction(
                funcCall.Name,
                argTypes,
                CurrentFunction.OwnerType,
                false,
                out func
            )
        )
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
            args[index] = args[index].ImplicitConvertTo(paramType);
            index++;
        }

        if (func is AspectMethod aspMethod)
        {
            if (toCallOn == null || toCallOn is not TraitPointer aspPtr)
            {
                throw new Exception("How did you get that method from that source"); //TODO
            }

            return aspPtr.CallMethod(this, aspMethod, args.ToArray());
        }

        return func.Call(args.ToArray());
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

    public TypeDecl GetType(string name)
    {
        if (CurrentNamespace.TryGetType(name, out TypeDecl type))
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

    public TraitDecl GetTrait(string name)
    {
        if (CurrentNamespace.TryGetTrait(name, out TraitDecl trait))
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

    public Function GetFunction(string name, IReadOnlyList<Data.Type> paramTypes)
    {
        if (
            CurrentFunction != null
            && CurrentFunction.OwnerType != null
            && CurrentFunction.OwnerType.TryGetFunction(
                name,
                paramTypes,
                CurrentFunction.OwnerType,
                true,
                out Function func
            )
        )
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

    public static IReadOnlyList<object> CleanAttributeArgs(ExpressionNode[] args)
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

        @namespace.Types.Add(Reserved.Void, Void);
        @namespace.Types.Add(Reserved.Float16, Float16);
        @namespace.Types.Add(Reserved.Float32, Float32);
        @namespace.Types.Add(Reserved.Float64, Float64);
        @namespace.Types.Add(Reserved.Bool, Bool);
        @namespace.Types.Add(Reserved.UInt8, UInt8);
        @namespace.Types.Add(Reserved.UInt16, UInt16);
        @namespace.Types.Add(Reserved.UInt32, UInt32);
        @namespace.Types.Add(Reserved.UInt64, UInt64);
        @namespace.Types.Add(Reserved.UInt128, UInt128);
        @namespace.Types.Add(Reserved.Int8, Int8);
        @namespace.Types.Add(Reserved.Int16, Int16);
        @namespace.Types.Add(Reserved.Int32, Int32);
        @namespace.Types.Add(Reserved.Int64, Int64);
        @namespace.Types.Add(Reserved.Int128, Int128);

        foreach (StructDecl @struct in @namespace.Types.Values)
            @struct.AddBuiltins();

        return @namespace;
    }

    private void AddDefaultForeigns()
    {
        Dictionary<string, FuncType> entries = new Dictionary<string, FuncType>();

        {
            entries.Add(
                Reserved.Malloc,
                new FuncType(this, new PtrType(this, Void), new Data.Type[1] { UInt64 }, false)
            );
            entries.Add(
                Reserved.Realloc,
                new FuncType(
                    this,
                    new PtrType(this, Void),
                    new Data.Type[2] { new PtrType(this, Void), UInt64 },
                    false
                )
            );
            entries.Add(
                Reserved.Free,
                new FuncType(this, Void, new Data.Type[1] { new PtrType(this, Void) }, false)
            );
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
            "llvm.powi.f32.i32" => new Pow(this, name, Module, Float32, Float32, Int32),
            "llvm.powi.f64.i16" => new Pow(this, name, Module, Float64, Float64, Int64),
            "llvm.pow.f32" => new Pow(this, name, Module, Float32, Float32, Float32),
            "llvm.pow.f64" => new Pow(this, name, Module, Float64, Float64, Float64),
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
            if (
                LLVMSharp.Interop.LLVM.CreateMemoryBufferWithContentsOfFile(
                    (sbyte*)ptr,
                    &memoryBuffer,
                    &message
                ) != 0
            )
                throw new Exception(new string(message));
            return memoryBuffer;
        }
    }
}
