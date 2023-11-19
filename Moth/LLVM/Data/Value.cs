namespace Moth.LLVM.Data;

public class Value : CompilerData
{
    public virtual Type Type { get; }
    public virtual LLVMValueRef LLVMValue { get; }

    public Value(Type type, LLVMValueRef value)
    {
        Type = type;
        LLVMValue = value;
    }
}

public class ClassValue : Value
{
    public ClassValue(Class type, LLVMValueRef value) : base(type, value) { }
}

public class FieldValue : Value
{
    public FieldValue(LLVMCompiler compiler, Field field, ClassValue owner)
        : base(field.Type,
            compiler.Builder.BuildStructGEP2(owner.Type.LLVMType,
                owner.LLVMValue,
                field.FieldIndex,
                field.Name)) { }
}