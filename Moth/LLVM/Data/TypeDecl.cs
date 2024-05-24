using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class TypeDecl : Type, IContainer
{
    public IContainer? Parent { get; }
    public Guid UUID { get; } = Guid.NewGuid();
    public string Name { get; }
    public PrivacyType Privacy { get; }
    public virtual bool IsUnion { get; }
    public Dictionary<string, IAttribute> Attributes { get; }
    public virtual Dictionary<string, OverloadList> Methods { get; } = new Dictionary<string, OverloadList>();
    public virtual Dictionary<string, OverloadList> StaticMethods { get; } = new Dictionary<string, OverloadList>();
    // public Dictionary<string, Property> Properties { get; } = new Dictionary<string, Property>();

    protected LLVMCompiler _compiler;
    private Func<LLVMCompiler, TypeDecl, LLVMTypeRef> _llvmTypeFn;
    private LLVMTypeRef _internalLLVMType;
    private TInfo? _internalTInfo;

    protected TypeDecl(LLVMCompiler compiler, Namespace parent, string name,
        Func<LLVMCompiler, TypeDecl, LLVMTypeRef> llvmTypeFn, PrivacyType privacy,
        Dictionary<string, IAttribute> attributes) : base(null, TypeKind.Decl)
    {
        _compiler = compiler;
        Parent = parent;
        Name = name;
        _llvmTypeFn = llvmTypeFn;
        Privacy = privacy;
        Attributes = attributes;
    }

    public virtual string FullName { get => $"{Parent.FullName}#{Name}"; }

    public override LLVMTypeRef LLVMType
    {
        get
        {
            if (_internalLLVMType == default)
                _internalLLVMType = _llvmTypeFn(_compiler, this);

            return _internalLLVMType;
        }
    }
    
    public TInfo? TInfo
    {
        get
        {
            if (IsExternal)
                return null;
            
            if (_internalTInfo == default)
                _internalTInfo = new TInfo(_compiler, this);

            return _internalTInfo;
        }
    }
    
    public virtual TypeDecl AddBuiltins()
    {
        // sizeof()
        {
            LLVMValueRef retValue = LLVMType.Kind == LLVMTypeKind.LLVMVoidTypeKind
                ? LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0)
                : LLVMType.SizeOf;

            var value = Value.Create(_compiler.UInt64, retValue);
            var func = new ConstRetFn(Reserved.SizeOf, value, _compiler.Module);
            StaticMethods.TryAdd(Reserved.SizeOf, new OverloadList(Reserved.SizeOf));
            StaticMethods[Reserved.SizeOf].Add(func);
        }

        // alignof()
        {
            LLVMValueRef retValue = LLVMType.Kind == LLVMTypeKind.LLVMVoidTypeKind
                ? LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 1)
                : LLVMType.AlignOf;

            var value = Value.Create(_compiler.UInt64, retValue);
            var func = new ConstRetFn(Reserved.AlignOf, value, _compiler.Module);
            StaticMethods.TryAdd(Reserved.AlignOf, new OverloadList(Reserved.AlignOf));
            StaticMethods[Reserved.AlignOf].Add(func);
        }

        return this;
    }
    
    public Function GetMethod(string name, IReadOnlyList<Type> paramTypes, TypeDecl? currentDecl)
    {
        if (Methods.TryGetValue(name, out OverloadList overloads)
            && overloads.TryGet(paramTypes, out Function func))
        {
            if (func is DefinedFunction defFunc && defFunc.Privacy == PrivacyType.Priv && currentDecl != this)
            {
                throw new Exception($"Cannot access private method \"{name}\" on type \"{Name}\".");
            }

            return func;
        }
        else
        {
            throw new Exception($"Method \"{name}\" does not exist on type \"{Name}\".");
        }
    }
    
    public override string ToString() => FullName;
    public override bool Equals(object? obj) => obj is TypeDecl type && UUID == type.UUID;
    public override int GetHashCode() => Name.GetHashCode() * Privacy.GetHashCode() * (int)LLVMType.Kind;
}
