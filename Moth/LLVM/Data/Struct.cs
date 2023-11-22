using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Struct : Type, IFieldContainer, IFunctionContainer
{
    public IContainer? Parent { get; }
    public string Name { get; }
    public PrivacyType Privacy { get; }
    public Dictionary<string, Field> Fields { get; } = new Dictionary<string, Field>();
    public Dictionary<Signature, Function> StaticMethods { get; } = new Dictionary<Signature, Function>();
    
    public Struct(IContainer? parent, string name, LLVMTypeRef llvmType, PrivacyType privacy)
        : base(llvmType, TypeKind.Class)
    {
        Parent = parent;
        Name = name;
        Privacy = privacy;
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
    
    public Field GetField(string key) => throw new NotImplementedException();
    
    public Function GetFunction(Signature sig) => throw new NotImplementedException();
    
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