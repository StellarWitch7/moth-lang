using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class EnumDecl : TypeDecl
{
    public Dictionary<string, EnumFlag> Flags { get; } = new Dictionary<string, EnumFlag>();

    private UnsignedInt _requiredSize;

    public EnumDecl(
        LLVMCompiler compiler,
        Namespace parent,
        string name,
        PrivacyType privacy,
        Dictionary<string, IAttribute> attributes
    )
        : base(
            compiler,
            parent,
            name,
            (decl) => (decl as EnumDecl).MakeLLVMTaggedUnionType(),
            privacy,
            attributes
        ) { }

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
            return _compiler.UInt128;
        else if (u32)
            return _compiler.UInt64;
        else if (u16)
            return _compiler.UInt32;
        else if (u8)
            return _compiler.UInt16;
        else
            return _compiler.UInt8;
    }

    public LLVMTypeRef MakeLLVMTaggedUnionType()
    {
        var llvmStruct = _compiler.Context.CreateNamedStruct($"__internal_{FullName}");
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

            if (!b)
                break;
            llvmTypes.Add(greatest.LLVMType);
            index++;
        }

        llvmStruct.StructSetBody(llvmTypes.ToArray(), false);
        return llvmStruct;
    }
}
