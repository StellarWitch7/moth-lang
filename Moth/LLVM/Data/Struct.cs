using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Struct : Type, IContainer
{
    public IContainer? Parent { get; }
    public string Name { get; }
    public PrivacyType Privacy { get; }
    public Dictionary<string, Field> Fields { get; } = new Dictionary<string, Field>();
    public Dictionary<Signature, Function> StaticMethods { get; } = new Dictionary<Signature, Function>();
    
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

    public virtual void AddBuiltins(LLVMCompiler compiler)
    {
        // sizeof()
        {
            LLVMValueRef retValue = LLVMType.Kind == LLVMTypeKind.LLVMVoidTypeKind
                ? LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0)
                : LLVMType.SizeOf;

            var value = new Value(Primitives.UInt64, retValue);
            var func = new ConstRetFn($"{Name}.{Reserved.SizeOf}", value, compiler.Module);
            StaticMethods.TryAdd(new Signature(Reserved.SizeOf, Array.Empty<Type>()), func);
        }

        // alignof()
        {
            LLVMValueRef retValue = LLVMType.Kind == LLVMTypeKind.LLVMVoidTypeKind
                ? LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 1)
                : LLVMType.AlignOf;

            var value = new Value(Primitives.UInt64, retValue);
            var func = new ConstRetFn($"{Name}.{Reserved.AlignOf}", value, compiler.Module);
            StaticMethods.TryAdd(new Signature(Reserved.AlignOf, Array.Empty<Type>()), func);
        }

        if (this.GetType() == typeof(Struct))
        {
            var ret = this;
            List<Type> @params = new List<Type>();

            foreach (Field field in Fields.Values)
            {
                if (field.Privacy != PrivacyType.Private)
                {
                    @params.Add(field.Type);
                }
            }
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

    public virtual Function GetFunction(Signature sig, Struct? currentStruct, bool recursive)
    {
        if (StaticMethods.TryGetValue(sig, out Function func))
        {
            if (func is DefinedFunction defFunc && defFunc.Privacy == PrivacyType.Private && currentStruct != this)
            {
                throw new Exception($"Cannot access private function \"{sig}\" on type \"{Name}\".");
            }

            return func;
        }
        else
        {
            if (recursive)
            {
                return ParentNamespace.GetFunction(sig);
            }
            else
            {
                throw new Exception($"Function \"{sig}\" does not exist on type \"{Name}\".");
            }
        }
    }

    public bool TryGetFunction(Signature sig, Struct? currentStruct, bool recursive, out Function func)
    {
        try
        {
            func = GetFunction(sig, currentStruct, recursive);

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
    
    public override string ToString() => Name;

    public override bool Equals(object? obj)
        => obj is Struct type
            && LLVMType.Kind == type.LLVMType.Kind
            && Kind == type.Kind
            && Name == type.Name;

    public override int GetHashCode() => Kind.GetHashCode() * Name.GetHashCode() * (int)LLVMType.Kind;
}

public class OpaqueStruct : Struct
{
    public OpaqueStruct(LLVMCompiler compiler, string name, PrivacyType privacy)
        : base(null, name, compiler.Context.CreateNamedStruct(name), privacy) { }

    public override void AddBuiltins(LLVMCompiler compiler) => throw new Exception("Cannot add builtins to opaque struct.");
}