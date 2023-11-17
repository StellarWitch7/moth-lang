using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Class : Type, IFunctionContainer
{
    public static Class Void = new Class(null, Reserved.Void, LLVMTypeRef.Void, PrivacyType.Public);
    
    public IContainer? Parent { get; }
    public string Name { get; }
    public PrivacyType Privacy { get; }
    public Dictionary<string, Field> Fields { get; } = new Dictionary<string, Field>();
    public Dictionary<Signature, Function> Methods { get; } = new Dictionary<Signature, Function>();
    public Dictionary<string, Field> StaticFields { get; } = new Dictionary<string, Field>();
    public Dictionary<Signature, Function> StaticMethods { get; } = new Dictionary<Signature, Function>();
    public Dictionary<string, Constant> Constants { get; } = new Dictionary<string, Constant>();

    public Class(IContainer? parent, string name, LLVMTypeRef llvmType, PrivacyType privacy)
        : base(llvmType, TypeKind.Class)
    {
        Parent = parent;
        Name = name;
        Privacy = privacy;
    }

    public void AddBuiltins(LLVMCompiler compiler)
    {
        // sizeof()
        {
            LLVMValueRef retValue = LLVMType.Kind == LLVMTypeKind.LLVMVoidTypeKind
                ? LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0)
                : LLVMType.SizeOf;

            var value = new Value(UnsignedInt.UInt64, retValue);
            var func = new ConstRetFn($"{Name}.{Reserved.SizeOf}", value, compiler.Module);
            StaticMethods.TryAdd(new Signature(Reserved.SizeOf, Array.Empty<Type>()), func);
        }

        // alignof()
        {
            LLVMValueRef retValue = LLVMType.Kind == LLVMTypeKind.LLVMVoidTypeKind
                ? LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 1)
                : LLVMType.AlignOf;

            var value = new Value(UnsignedInt.UInt64, retValue);
            var func = new ConstRetFn($"{Name}.{Reserved.AlignOf}", value, compiler.Module);
            StaticMethods.TryAdd(new Signature(Reserved.AlignOf, Array.Empty<Type>()), func);
        }
    }

    public Function GetFunction(Signature sig) => throw new NotImplementedException();

    public bool TryGetFunction(Signature sig, out Function func) => throw new NotImplementedException();

    public CompilerData GetData(string name) => throw new NotImplementedException();

    public bool TryGetData(string name, out CompilerData data) => throw new NotImplementedException();
    
    public override string ToString() => Name;

    public override bool Equals(object? obj)
        => obj is Class type
            && LLVMType.Kind == type.LLVMType.Kind
            && Kind == type.Kind
            && Name == type.Name;

    public override int GetHashCode() => Kind.GetHashCode() * Name.GetHashCode() * (int)LLVMType.Kind;
}
