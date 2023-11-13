using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Class : CompilerData, IFunctionContainer
{
    public IContainer? Parent { get; }
    public string Name { get; }
    public ClassType Type { get; }
    public PrivacyType Privacy { get; }
    public Dictionary<string, Field> Fields { get; } = new Dictionary<string, Field>();
    public Dictionary<Signature, FuncVal> Methods { get; } = new Dictionary<Signature, FuncVal>();
    public Dictionary<string, Field> StaticFields { get; } = new Dictionary<string, Field>();
    public Dictionary<Signature, FuncVal> StaticMethods { get; } = new Dictionary<Signature, FuncVal>();
    public Dictionary<string, Constant> Constants { get; } = new Dictionary<string, Constant>();

    public Class(IContainer? parent, string name, LLVMTypeRef llvmType, PrivacyType privacy)
    {
        Parent = parent;
        Name = name;
        Type = new ClassType(llvmType, this, TypeKind.Class);
        Privacy = privacy;
    }

    public void AddBuiltins(LLVMCompiler compiler)
    {
        // sizeof()
        {
            LLVMValueRef retValue = Type.LLVMType.Kind == LLVMTypeKind.LLVMVoidTypeKind
                ? LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0)
                : Type.LLVMType.SizeOf;

            var value = new Value(UnsignedInt.UInt64.Type, retValue);
            var func = new ConstRetFn($"{Name}.{Reserved.SizeOf}", value);
            StaticMethods.TryAdd(new Signature(Reserved.SizeOf, Array.Empty<ClassType>()), func);
        }

        // alignof()
        {
            LLVMValueRef retValue = Type.LLVMType.Kind == LLVMTypeKind.LLVMVoidTypeKind
                ? LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 1)
                : Type.LLVMType.AlignOf;

            var value = new Value(UnsignedInt.UInt64.Type, retValue);
            var func = new ConstRetFn($"{Name}.{Reserved.AlignOf}", compiler.Module, value);
            StaticMethods.TryAdd(new Signature(Reserved.AlignOf, Array.Empty<ClassType>()), func);
        }
    }

    public FuncType GetFunction(Signature sig) => throw new NotImplementedException();

    public bool TryGetFunction(Signature sig, out FuncType func) => throw new NotImplementedException();

    public CompilerData GetData(string name) => throw new NotImplementedException();

    public bool TryGetData(string name, out CompilerData data) => throw new NotImplementedException();
}
