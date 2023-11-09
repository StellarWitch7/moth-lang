using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Class : CompilerData
{
    public string Name { get; set; }
    public Type Type { get; set; }
    public PrivacyType Privacy { get; set; }
    public Dictionary<string, Field> Fields { get; set; } = new Dictionary<string, Field>();
    public FuncDictionary Methods { get; set; } = new FuncDictionary();
    public Dictionary<string, Field> StaticFields { get; set; } = new Dictionary<string, Field>();
    public FuncDictionary StaticMethods { get; set; } = new FuncDictionary();
    public Dictionary<string, Constant> Constants { get; set; } = new Dictionary<string, Constant>();

    public Class(string name, LLVMTypeRef llvmType, PrivacyType privacy)
    {
        Name = name;
        Type = new Type(llvmType, this, TypeKind.Class);
        Privacy = privacy;
    }

    public Field GetField(string name)
    {
        return Fields.TryGetValue(name, out Field? field)
            ? field
            : throw new Exception($"Field \"{name}\" does not exist on class \"{Name}\"!");
    }

    public Field GetStaticField(string name)
    {
        return StaticFields.TryGetValue(name, out Field? field)
            ? field
            : throw new Exception($"Static field \"{name}\" does not exist on class \"{Name}\"!");
    }

    public Function GetMethod(Signature sig)
    {
        return Methods.TryGetValue(sig, out Function? func)
            ? func
            : throw new Exception($"Method \"{sig}\" does not exist on class \"{Name}\"!");
    }

    public Function GetStaticMethod(Signature sig)
    {
        return StaticMethods.TryGetValue(sig, out Function? func)
            ? func
            : throw new Exception($"Static method \"{sig}\" does not exist on class \"{Name}\"!");
    }

    public void AddBuiltins(LLVMCompiler compiler)
    {
        // sizeof()
        {
            LLVMValueRef retValue = Type.LLVMType.Kind == LLVMTypeKind.LLVMVoidTypeKind
                ? LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0)
                : Type.LLVMType.SizeOf;

            var value = new ValueContext(UnsignedInt.UInt64.Type, retValue);
            var func = new ConstRetFn($"{Name}.{Reserved.SizeOf}", compiler.Module, value);
            StaticMethods.TryAdd(new Signature(Reserved.SizeOf, Array.Empty<Type>()), func);
        }

        // alignof()
        {
            LLVMValueRef retValue = Type.LLVMType.Kind == LLVMTypeKind.LLVMVoidTypeKind
                ? LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 1)
                : Type.LLVMType.AlignOf;

            var value = new ValueContext(UnsignedInt.UInt64.Type, retValue);
            var func = new ConstRetFn($"{Name}.{Reserved.AlignOf}", compiler.Module, value);
            StaticMethods.TryAdd(new Signature(Reserved.AlignOf, Array.Empty<Type>()), func);
        }
    }
}
