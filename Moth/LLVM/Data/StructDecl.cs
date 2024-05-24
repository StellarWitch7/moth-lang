using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class StructDecl : TypeDecl
{
    public virtual Dictionary<string, Field> Fields { get; } = new Dictionary<string, Field>();
    public Dictionary<TraitDecl, VTableInst> VTables { get; } = new Dictionary<TraitDecl, VTableInst>();
    public ScopeNode? Scope { get; }

    protected ImplicitConversionTable _internalImplicits;
    private uint _bitlength;

    public StructDecl(LLVMCompiler compiler, Namespace? parent, string name, PrivacyType privacy,
        bool isUnion, Dictionary<string, IAttribute> attributes, ScopeNode? scope, LLVMTypeRef preBuiltLLVMType = default)
        : base(compiler, parent, name, preBuiltLLVMType == default ? Compile : (llvmCompiler, decl) => preBuiltLLVMType, privacy, attributes)
    {
        Scope = scope;
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
                if (IsUnion)
                {
                    throw new NotImplementedException(); //TODO
                }
                else
                {
                    uint i = 0;

                    foreach (var field in Fields.Values)
                    {
                        i += field.Type.Bits;
                    }

                    _bitlength = i;
                }
            }

            return _bitlength;
        }
    }

    public static LLVMTypeRef Compile(LLVMCompiler compiler, TypeDecl typeDecl)
    {
        var structDecl = typeDecl as StructDecl;
        var fieldTypes = new List<InternalType>();
        uint index = 0;

        foreach (FieldDefNode field in structDecl.Scope.Statements.OfType<FieldDefNode>())
        {
            InternalType fieldType = compiler.ResolveType(field.TypeRef);
            structDecl.Fields.Add(field.Name, new Field(compiler, structDecl, field.Name, index, fieldType, field.Privacy));

            if (structDecl.IsUnion)
            {
                if (fieldTypes.Count == 0)
                {
                    fieldTypes.Add(fieldType);
                }
                else
                {
                    var currentLargest = fieldTypes[0];

                    if (fieldType.Bits > currentLargest.Bits)
                        fieldTypes[0] = fieldType;
                }
            }
            else
            {
                fieldTypes.Add(fieldType);
                index++;
            }
        }

        var llvmType = compiler.Context.CreateNamedStruct(structDecl.FullName);
        llvmType.StructSetBody(fieldTypes.AsLLVMTypes().AsReadonlySpan(), false);
        return llvmType;
    }
    
    public bool Implements(TraitDecl traitDecl) => VTables.Keys.Contains(traitDecl);
    
    public override ImplicitConversionTable GetImplicitConversions()
    {
        if (_internalImplicits == default)
            _internalImplicits = new ImplicitConversionTable(_compiler);
        
        return _internalImplicits;
    }

    public Field GetField(string name, TypeDecl currentTypeDecl)
    {
        if (Fields.TryGetValue(name, out Field field))
        {
            if (field.Privacy == PrivacyType.Priv && currentTypeDecl != this)
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
    
    public bool TryGetField(string name, StructDecl currentStructDecl, out Field field)
    {
        try
        {
            field = GetField(name, currentStructDecl);

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

    public virtual Variable Init()
    {
        return new Variable(_compiler, Reserved.Self,
            new VarType(_compiler, this),
            _compiler.Builder.BuildAlloca(LLVMType));
    }
}

public class OpaqueStructDecl : StructDecl
{
    public OpaqueStructDecl(LLVMCompiler compiler, Namespace parent, string name, PrivacyType privacy, bool isUnion, Dictionary<string, IAttribute> attributes)
        : base(compiler, parent, name, privacy, isUnion, attributes, null) { }
    
    public override LLVMTypeRef LLVMType { get => LLVMTypeRef.Int8; }

    public override StructDecl AddBuiltins() => this;
}