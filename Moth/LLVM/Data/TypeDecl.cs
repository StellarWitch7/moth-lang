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
    public virtual Dictionary<string, OverloadList> Methods { get; } =
        new Dictionary<string, OverloadList>();
    public virtual Dictionary<string, OverloadList> StaticMethods { get; } =
        new Dictionary<string, OverloadList>();

    // public Dictionary<string, Property> Properties { get; } = new Dictionary<string, Property>();

    private Func<TypeDecl, LLVMTypeRef> _llvmTypeFn;
    private LLVMTypeRef _internalLLVMType;
    private TInfo? _internalTInfo;
    private Version? _internalVersionOverride;

    protected TypeDecl(
        LLVMCompiler compiler,
        Namespace parent,
        string name,
        Func<TypeDecl, LLVMTypeRef> llvmTypeFn,
        PrivacyType privacy,
        Dictionary<string, IAttribute> attributes
    )
        : base(compiler, null, TypeKind.Decl)
    {
        Parent = parent;
        Name = name;
        _llvmTypeFn = llvmTypeFn;
        Privacy = privacy;
        Attributes = attributes;

        if (_compiler.Options.DoExport && attributes.ContainsKey(Reserved.Export))
        {
            if (this is StructDecl structDecl)
            {
                _compiler.Header.Structs.Add(structDecl);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }

    public Version OriginModuleVersion
    {
        get
        {
            if (_internalVersionOverride != null)
                return (Version)_internalVersionOverride;

            return _compiler.ModuleVersion;
        }
        set { _internalVersionOverride = value; }
    }

    public virtual string FullName
    {
        get
        {
            if (Parent == null)
                return $"#{Name}";
            else
                return $"{Parent.FullName}#{Name}";
        }
    }

    public Namespace ParentNamespace
    {
        get
        {
            if (Parent == null)
            {
                throw new Exception(
                    $"Type \"{Name}\" has no parent namespace. "
                        + $"This is a fatal compiler error, report ASAP."
                );
            }

            return Parent is Namespace nmspace
                ? nmspace
                : throw new Exception(
                    $"Type \"{Name}\" has an incorrect parent. "
                        + $"This is a fatal compiler error, report ASAP."
                );
        }
    }
    public override LLVMTypeRef LLVMType
    {
        get
        {
            if (_internalLLVMType == default)
                _internalLLVMType = _llvmTypeFn(this);

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
            LLVMValueRef retValue =
                LLVMType.Kind == LLVMTypeKind.LLVMVoidTypeKind
                    ? LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0)
                    : LLVMType.SizeOf;

            var value = Value.Create(_compiler, _compiler.UInt64, retValue);
            var func = new ConstRetFn(_compiler, Reserved.SizeOf, value, _compiler.Module);
            StaticMethods.TryAdd(Reserved.SizeOf, new OverloadList(Reserved.SizeOf));
            StaticMethods[Reserved.SizeOf].Add(func);
        }

        // alignof()
        {
            LLVMValueRef retValue =
                LLVMType.Kind == LLVMTypeKind.LLVMVoidTypeKind
                    ? LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 1)
                    : LLVMType.AlignOf;

            var value = Value.Create(_compiler, _compiler.UInt64, retValue);
            var func = new ConstRetFn(_compiler, Reserved.AlignOf, value, _compiler.Module);
            StaticMethods.TryAdd(Reserved.AlignOf, new OverloadList(Reserved.AlignOf));
            StaticMethods[Reserved.AlignOf].Add(func);
        }

        return this;
    }

    public Function GetMethod(string name, IReadOnlyList<Type> paramTypes, TypeDecl? currentDecl)
    {
        if (
            Methods.TryGetValue(name, out OverloadList overloads)
            && overloads.TryGet(paramTypes, out Function func)
        )
        {
            if (
                func is DefinedFunction defFunc
                && defFunc.Privacy == PrivacyType.Priv
                && currentDecl != this
            )
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

    public virtual Function GetFunction(
        string name,
        IReadOnlyList<Type> paramTypes,
        TypeDecl? currentTypeDecl,
        bool recursive
    )
    {
        if (
            StaticMethods.TryGetValue(name, out OverloadList overloads)
            && overloads.TryGet(paramTypes, out Function func)
        )
        {
            if (
                func is DefinedFunction defFunc
                && defFunc.Privacy == PrivacyType.Priv
                && currentTypeDecl != this
            )
            {
                throw new Exception(
                    $"Cannot access private function \"{name}\" on type \"{Name}\"."
                );
            }

            return func;
        }
        else
        {
            if (recursive)
            {
                return ParentNamespace.GetFunction(name, paramTypes);
            }
            else
            {
                throw new Exception($"Function \"{name}\" does not exist on type \"{Name}\".");
            }
        }
    }

    public bool TryGetFunction(
        string name,
        IReadOnlyList<Type> paramTypes,
        TypeDecl? currentTypeDecl,
        bool recursive,
        out Function func
    )
    {
        try
        {
            func = GetFunction(name, paramTypes, currentTypeDecl, recursive);

            if (func == null)
            {
                throw new Exception();
            }

            return true;
        }
        catch
        {
            func = null;
            return false;
        }
    }

    public override string ToString() => FullName;

    public override bool Equals(object? obj) => obj is TypeDecl type && UUID == type.UUID;

    public override int GetHashCode() => Name.GetHashCode() * Privacy.GetHashCode();
}
