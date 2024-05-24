using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class EnumDecl : TypeDecl<EnumDecl>
{
    public Dictionary<string, EnumFlag> Flags { get; } = new Dictionary<string, EnumFlag>();

    private UnsignedInt _requiredSize;

    public EnumDecl(LLVMCompiler compiler, Namespace parent, string name, PrivacyType privacy, Dictionary<string, IAttribute> attributes)
        : base(compiler, parent, name, (llvmCompiler, decl) => decl.MakeLLVMTaggedUnionType(llvmCompiler), privacy, attributes) { }

    public UnsignedInt FlagType
    {
        get
        {
            if (_requiredSize == default)
                _requiredSize = RequiredSize();
            
            return _requiredSize;
        }
    }

    private UnsignedInt RequiredSize() //TODO: this should default to usize/size_t/ptrsize
    {
        bool u8 = false;
        bool u16 = false;
        bool u32 = false;
        bool u64 = false;
        
        foreach (var flag in Flags.Values)
        {
            if (flag.Value > Byte.MaxValue)
                u8 = true;

            if (flag.Value > UInt16.MaxValue)
                u16 = true;

            if (flag.Value > UInt32.MaxValue)
                u32 = true;

            if (flag.Value > UInt64.MaxValue)
                u64 = true;
        }

        if (u64)
            return Primitives.UInt128;
        else if (u32)
            return Primitives.UInt64;
        else if (u16)
            return Primitives.UInt32;
        else if (u8)
            return Primitives.UInt16;
        else
            return Primitives.UInt8;
    }
    
    private LLVMTypeRef MakeLLVMTaggedUnionType(LLVMCompiler compiler)
    {
        var llvmStruct = compiler.Context.CreateNamedStruct($"__internal_{FullName}");
        var llvmTypes = new List<LLVMTypeRef>() { FlagType.LLVMType };
        int index = 0;

        while (true)
        {
            Type greatest = null;
            bool b = false;
            
            foreach (var flag in Flags.Values.OfType<UnionEnumFlag>())
            {
                if (flag.UnionTypes.Count > index)
                    b = true;

                var t = flag.UnionTypes[index];

                if (greatest == null)
                    greatest = t;
                else if (t.Bits > greatest.Bits)
                    greatest = t;
            }

            if (!b) break;
            llvmTypes.Add(greatest.LLVMType);
            index++;
        }
        
        llvmStruct.StructSetBody(llvmTypes.ToArray(), false);
        return llvmStruct;
    }
}
