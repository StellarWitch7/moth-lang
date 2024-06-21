using Moth.AST;
using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Field : IContainer
{
    public IContainer? Parent { get; }
    public string Name { get; }
    public uint FieldIndex { get; }
    public Type Type { get; }
    public PrivacyType Privacy { get; }
    public bool IsExternal
    {
        get => Parent.IsExternal;
        init { }
    }
    public string FullName
    {
        get => $"{Parent.FullName}.{Name}";
    }

    private LLVMCompiler _compiler;

    public Field(
        LLVMCompiler compiler,
        StructDecl parent,
        string name,
        uint index,
        Type type,
        PrivacyType privacy
    )
    {
        _compiler = compiler;
        Parent = parent;
        Name = name;
        FieldIndex = index;
        Type = type;
        Privacy = privacy;
    }

    public Pointer GetValue(Value parent)
    {
        return new Pointer(
            _compiler,
            new VarType(_compiler, Type),
            _compiler.Builder.BuildStructGEP2(
                (Parent as StructDecl).LLVMType,
                parent.LLVMValue,
                FieldIndex,
                Name
            )
        );
    }
}
