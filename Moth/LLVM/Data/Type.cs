using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Type : InternalType, IContainer
{
    public IContainer? Parent { get; }
    public Guid UUID { get; }
    public string Name { get; }
    public TInfo? TInfo { get; } = null;
    public Dictionary<string, IAttribute> Attributes { get; }
    public PrivacyType Privacy { get; }
    public virtual Dictionary<string, Field> Fields { get; } = new Dictionary<string, Field>();
    public virtual Dictionary<string, OverloadList> Methods { get; } = new Dictionary<string, OverloadList>();
    public virtual Dictionary<string, OverloadList> StaticMethods { get; } = new Dictionary<string, OverloadList>();
    public Dictionary<Trait, VTableInst> VTables { get; } = new Dictionary<Trait, VTableInst>();

    protected ImplicitConversionTable internalImplicits = null;
    
    private uint _bitlength;
    
    public Type(LLVMCompiler? compiler, Namespace? parent, string name, LLVMTypeRef llvmType, Dictionary<string, IAttribute> attributes, PrivacyType privacy)
        : base(llvmType, TypeKind.Struct)
    {
        UUID = Guid.NewGuid();
        Parent = parent;
        Name = name;
        Attributes = attributes;
        Privacy = privacy;

        if (compiler != null)
            TInfo = new TInfo(compiler, this);
    }

    public Namespace ParentNamespace
    {
        get
        {
            if (Parent == null)
            {
                throw new Exception($"Type \"{Name}\" has no parent namespace. " +
                    $"This is a fatal compiler error, report ASAP.");
            }
            
            return Parent is Namespace nmspace
                ? nmspace
                : throw new Exception($"Type \"{Name}\" has an incorrect parent. " +
                    $"This is a fatal compiler error, report ASAP.");
        }
    }

    public virtual string FullName
    {
        get
        {
            return $"{Parent.FullName}#{Name}";
        }
    }

    public override uint Bits
    {
        get
        {
            if (_bitlength == default)
            {
                uint i = 0;

                foreach (var field in Fields.Values)
                {
                    i += field.InternalType.Bits;
                }

                _bitlength = i;
            }

            return _bitlength;
        }
    }

    public bool Implements(Trait trait) => VTables.Keys.Contains(trait);
    
    public override ImplicitConversionTable GetImplicitConversions()
    {
        if (internalImplicits == null)
        {
            internalImplicits = new ImplicitConversionTable();
        }
        
        return internalImplicits;
    }

    public virtual Type AddBuiltins(LLVMCompiler compiler)
    {
        // sizeof()
        {
            LLVMValueRef retValue = LLVMType.Kind == LLVMTypeKind.LLVMVoidTypeKind
                ? LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0)
                : LLVMType.SizeOf;

            var value = Value.Create(Primitives.UInt64, retValue);
            var func = new ConstRetFn(Reserved.SizeOf, value, compiler.Module);
            StaticMethods.TryAdd(Reserved.SizeOf, new OverloadList(Reserved.SizeOf));
            StaticMethods[Reserved.SizeOf].Add(func);
        }

        // alignof()
        {
            LLVMValueRef retValue = LLVMType.Kind == LLVMTypeKind.LLVMVoidTypeKind
                ? LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 1)
                : LLVMType.AlignOf;

            var value = Value.Create(Primitives.UInt64, retValue);
            var func = new ConstRetFn(Reserved.AlignOf, value, compiler.Module);
            StaticMethods.TryAdd(Reserved.AlignOf, new OverloadList(Reserved.AlignOf));
            StaticMethods[Reserved.AlignOf].Add(func);
        }

        return this;
    }
    
    public Function GetMethod(string name, IReadOnlyList<InternalType> paramTypes, Type? currentStruct)
    {
        if (Methods.TryGetValue(name, out OverloadList overloads)
            && overloads.TryGet(paramTypes, out Function func))
        {
            if (func is DefinedFunction defFunc && defFunc.Privacy == PrivacyType.Private && currentStruct != this)
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

    public Field GetField(string name, Type currentType)
    {
        if (Fields.TryGetValue(name, out Field field))
        {
            if (field.Privacy == PrivacyType.Private && currentType != this)
            {
                throw new Exception($"Cannot access private field \"{name}\" on type \"{Name}\".");
            }

            return field;
        }
        else
        {
            throw new Exception($"Field \"{name}\" does not exist on type \"{Name}\".");
        }
    }
    
    public bool TryGetField(string name, Type currentType, out Field field)
    {
        try
        {
            field = GetField(name, currentType);

            if (field == null)
            {
                throw new Exception();
            }
            
            return true;
        }
        catch
        {
            field = null;
            return false;
        }
    }

    public virtual Function GetFunction(string name, IReadOnlyList<InternalType> paramTypes, Type? currentStruct, bool recursive)
    {
        if (StaticMethods.TryGetValue(name, out OverloadList overloads)
            && overloads.TryGet(paramTypes, out Function func))
        {
            if (func is DefinedFunction defFunc && defFunc.Privacy == PrivacyType.Private && currentStruct != this)
            {
                throw new Exception($"Cannot access private function \"{name}\" on type \"{Name}\".");
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

    public bool TryGetFunction(string name, IReadOnlyList<InternalType> paramTypes, Type? currentStruct, bool recursive, out Function func)
    {
        try
        {
            func = GetFunction(name, paramTypes, currentStruct, recursive);

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

    public virtual Variable Init(LLVMCompiler compiler)
    {
        return new Variable(Reserved.Self,
            new VarType(this),
            compiler.Builder.BuildAlloca(LLVMType));
    }
    
    public override string ToString() => FullName;

    public override bool Equals(object? obj)
        => obj is Type type
            && LLVMType.Kind == type.LLVMType.Kind
            && Kind == type.Kind
            && Name == type.Name;

    public override int GetHashCode() => Kind.GetHashCode() * Name.GetHashCode() * (int)LLVMType.Kind;
}

public class OpaqueType : Type
{
    public OpaqueType(LLVMCompiler compiler, Namespace parent, string name, Dictionary<string, IAttribute> attributes, PrivacyType privacy)
        : base(compiler, parent, name, compiler.Context.CreateNamedStruct(name), attributes, privacy) { }

    public override Type AddBuiltins(LLVMCompiler compiler) => this;
}