using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Function : Value
{
    public override FuncType Type { get; }
    public Parameter[] Params { get; }
    public Scope? OpeningScope { get; set; }

    public Function(LLVMCompiler compiler, FuncType type, LLVMValueRef value, Parameter[] @params)
        : base(compiler, type, value)
    {
        Type = type;
        Params = @params;
    }

    public string Name
    {
        get { return this is DefinedFunction definedFunc ? definedFunc.Name : "N/A"; }
    }

    public string FullName
    {
        get { return this is DefinedFunction definedFunc ? definedFunc.FullName : "N/A"; }
    }

    public Type ReturnType
    {
        get { return Type.ReturnType; }
    }

    public Type[] ParameterTypes
    {
        get { return Type.ParameterTypes; }
    }

    public bool IsVariadic
    {
        get { return Type.IsVariadic; }
    }

    public TypeDecl? OwnerType
    {
        get { return Type is MethodType methodType ? methodType.OwnerTypeDecl : null; }
    }

    public bool IsStatic
    {
        get { return Type is MethodType methodType ? methodType.IsStatic : true; }
    }

    public virtual Value Call(Value[] args) => Type.Call(LLVMValue, args);
}

public class DefinedFunction : Function
{
    public string Name { get; }
    public IContainer? Parent { get; }
    public PrivacyType Privacy { get; }
    public bool IsForeign { get; }
    public Dictionary<string, IAttribute> Attributes { get; }

    private LLVMValueRef _internalValue;

    public DefinedFunction(
        LLVMCompiler compiler,
        IContainer? parent,
        string name,
        FuncType type,
        Parameter[] @params,
        PrivacyType privacy,
        bool isForeign,
        Dictionary<string, IAttribute> attributes
    )
        : base(compiler, type, null, @params)
    {
        Name = name;
        Parent = parent;
        Privacy = privacy;
        IsForeign = isForeign;
        Attributes = attributes;
    }

    public override LLVMValueRef LLVMValue
    {
        get
        {
            if (_internalValue == default)
            {
                string llvmFuncName = !(Name == Reserved.Main || IsForeign) ? FullName : Name;
                _internalValue = IsForeign
                    ? _compiler.HandleForeign(Name, Type)
                    : _compiler.Module.AddFunction(llvmFuncName, Type.BaseType.LLVMType);

                if (
                    _compiler.Options.DoExport
                    && !IsForeign
                    && Attributes.ContainsKey(Reserved.Export)
                )
                {
                    foreach (var lang in _compiler.Options.ExportLanguages)
                    {
                        string stubName = Utils.MakeStubName(lang, llvmFuncName);
                        FuncType stubType = Utils.MakeStubType(_compiler, lang, Type);

                        if (_compiler.Module.GetNamedFunction(stubName) != null)
                            throw new Exception(
                                $"Cannot export overload of function \"{FullName}\"."
                            );

                        var stub = _compiler.Module.AddFunction(
                            stubName,
                            stubType.BaseType.LLVMType
                        );

                        using (var builder = _compiler.Context.CreateBuilder())
                        {
                            builder.PositionAtEnd(stub.AppendBasicBlock("entry"));

                            var ret = builder.BuildCall2(
                                stubType.BaseType.LLVMType,
                                _internalValue,
                                stub.Params
                            );

                            if (stubType.ReturnType.Equals(_compiler.Void))
                                builder.BuildRetVoid();
                            else
                                builder.BuildRet(ret);
                        }
                    }
                }
            }

            return _internalValue;
        }
    }

    public string FullName
    {
        get
        {
            if (Parent is StructDecl @struct)
            {
                return $"{@struct.FullName}.{Name}{Type}";
            }
            else if (Parent is Namespace nmspace)
            {
                return $"{nmspace.FullName}.{Name}{Type}";
            }
            else
            {
                throw new Exception(
                    "Parent of defined function is neither a type nor a namespace."
                );
            }
        }
    }
}

public class AspectMethod : DefinedFunction
{
    public AspectMethod(
        LLVMCompiler compiler,
        TraitDecl parent,
        string name,
        FuncType type,
        PrivacyType privacy,
        Dictionary<string, IAttribute> attributes
    )
        : base(compiler, parent, name, type, new Parameter[0], PrivacyType.Pub, false, attributes)
    { }

    public override LLVMValueRef LLVMValue
    {
        get { throw new Exception("Unimplemented aspect method has no internal LLVM value."); }
    }
}

public abstract class IntrinsicFunction : Function
{
    public string Name { get; }

    private LLVMValueRef _internalValue;

    public IntrinsicFunction(LLVMCompiler compiler, string name, FuncType type)
        : base(compiler, type, default, new Parameter[0])
    {
        Name = name;
    }

    public override LLVMValueRef LLVMValue
    {
        get
        {
            if (_internalValue == default)
            {
                _internalValue = GenerateLLVMData();
            }

            return _internalValue;
        }
    }

    protected virtual LLVMValueRef GenerateLLVMData() =>
        throw new NotImplementedException("This function does not support LLVM data generation.");
}
