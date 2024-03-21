using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Struct : Type, IContainer
{
    public IContainer? Parent { get; }
    public string Name { get; }
    public PrivacyType Privacy { get; }
    public Dictionary<string, Field> Fields { get; } = new Dictionary<string, Field>();
    public Dictionary<string, OverloadList> Methods { get; } = new Dictionary<string, OverloadList>();
    public Dictionary<string, OverloadList> StaticMethods { get; } = new Dictionary<string, OverloadList>();

    private uint _bitlength;
    
    public Struct(Namespace? parent, string name, LLVMTypeRef llvmType, PrivacyType privacy)
        : base(llvmType, TypeKind.Class)
    {
        Parent = parent;
        Name = name;
        Privacy = privacy;
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
            return $"{ParentNamespace.FullName}#{Name}";
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
                    i += field.Type.Bits;
                }

                _bitlength = i;
            }

            return _bitlength;
        }
    }

    public virtual Struct AddBuiltins(LLVMCompiler compiler)
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
    
    public Function GetMethod(string name, IReadOnlyList<Type> paramTypes, Struct? currentStruct)
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

    public Field GetField(string name, Struct currentStruct)
    {
        if (Fields.TryGetValue(name, out Field field))
        {
            if (field.Privacy == PrivacyType.Private && currentStruct != this)
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
    
    public bool TryGetField(string name, Struct currentStruct, out Field field)
    {
        try
        {
            field = GetField(name, currentStruct);

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

    public virtual Function GetFunction(string name, IReadOnlyList<Type> paramTypes, Struct? currentStruct, bool recursive)
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

    public bool TryGetFunction(string name, IReadOnlyList<Type> paramTypes, Struct? currentStruct, bool recursive, out Function func)
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
            compiler.WrapAsRef(this),
            compiler.Builder.BuildAlloca(LLVMType));
    }
    
    public override string ToString() => FullName;

    public override bool Equals(object? obj)
        => obj is Struct type
            && LLVMType.Kind == type.LLVMType.Kind
            && Kind == type.Kind
            && Name == type.Name;

    public override int GetHashCode() => Kind.GetHashCode() * Name.GetHashCode() * (int)LLVMType.Kind;
}

public class OpaqueStruct : Struct
{
    public OpaqueStruct(LLVMCompiler compiler, Namespace parent, string name, PrivacyType privacy)
        : base(parent, name, compiler.Context.CreateNamedStruct(name), privacy) { }

    public override Struct AddBuiltins(LLVMCompiler compiler) => this;
}