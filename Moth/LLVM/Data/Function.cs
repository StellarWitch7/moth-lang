using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Function : Value
{
    public override FuncType Type { get; }
    public Parameter[] Params { get; }
    public Scope? OpeningScope { get; set; }

    public Function(FuncType type, LLVMValueRef value, Parameter[] @params) : base(type, value)
    {
        Type = type;
        Params = @params;
    }

    public string Name
    {
        get
        {
            return this is DefinedFunction definedFunc ? definedFunc.Name : "N/A";
        }
    }

    public string FullName
    {
        get
        {
            return this is DefinedFunction definedFunc ? definedFunc.FullName : "N/A";
        }
    }
    
    public Type ReturnType
    {
        get
        {
            return Type.ReturnType;
        }
    }

    public Type[] ParameterTypes
    {
        get
        {
            return Type.ParameterTypes;
        }
    }

    public bool IsVariadic
    {
        get
        {
            return Type.IsVariadic;
        }
    }
    
    public Struct? OwnerStruct
    {
        get
        {
            return Type is MethodType methodType ? methodType.OwnerStruct : null;
        }
    }

    public bool IsStatic
    {
        get
        {
            return Type is MethodType methodType ? methodType.IsStatic : true;
        }
    }

    public virtual Value Call(LLVMCompiler compiler, Value[] args) => Type.Call(compiler, LLVMValue, args);
}

public class DefinedFunction : Function
{
    public string Name { get; }
    public IContainer? Parent { get; }
    public PrivacyType Privacy { get; }
    public bool IsForeign { get; }
    public Dictionary<string, IAttribute> Attributes { get; }

    private LLVMCompiler _compiler { get; }
    
    private LLVMValueRef _internalValue;
    
    public DefinedFunction(LLVMCompiler compiler, IContainer? parent, string name,
        FuncType type, Parameter[] @params, PrivacyType privacy,
        bool isForeign, Dictionary<string, IAttribute> attributes)
        : base(type, null, @params)
    {
        _compiler = compiler;
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
                string llvmFuncName = !(Name == Reserved.Main || IsForeign)
                    ? FullName
                    : Name;
                _internalValue = IsForeign
                    ? _compiler.HandleForeign(Name, Type)
                    : _compiler.Module.AddFunction(llvmFuncName, Type.BaseType.LLVMType);
            }

            return _internalValue;
        }
    }

    public string FullName
    {
        get
        {
            if (Parent is Struct @struct)
            {
                return $"{@struct.FullName}.{Name}{Type}";
            }
            else if (Parent is Namespace nmspace)
            {
                return $"{nmspace.FullName}.{Name}{Type}";
            }
            else
            {
                throw new Exception("Parent of defined function is neither a type nor a namespace.");
            }
        }
    }
}

public abstract class IntrinsicFunction : Function
{
    public string Name { get; }
    
    private LLVMValueRef _internalValue;

    public IntrinsicFunction(string name, FuncType type) : base(type, default, new Parameter[0])
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
    
    public override Value Call(LLVMCompiler compiler, Value[] args)
        => Type.Call(compiler, LLVMValue, args);
    
    protected virtual LLVMValueRef GenerateLLVMData()
        => throw new NotImplementedException("This function does not support LLVM data generation.");
}