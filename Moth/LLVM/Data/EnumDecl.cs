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

    public override ImplicitConversionTable GetImplicitConversions()
    {
        var table = new ImplicitConversionTable(_compiler);

        table.Add(
            FlagType,
            prev =>
            {
                return Value.Create(
                    _compiler,
                    FlagType,
                    _compiler.Builder.BuildExtractElement(
                        prev.LLVMValue,
                        LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0)
                    )
                );
            }
        );

        return table;
    }

    public Value MakeValue(string flagName, IReadOnlyList<Value> args = null) =>
        MakeValue(Flags[flagName], args);

    public Value MakeValue(EnumFlag flag, IReadOnlyList<Value> args = null)
    {
        args = args ?? new List<Value>();

        if (args.Count > 0)
        {
            if (flag is not UnionEnumFlag unionEnumFlag)
                throw new Exception(
                    $"Cannot make enum value \"{flag.Name}\" as it cannot hold values."
                );

            if (unionEnumFlag.UnionTypes.Count != args.Count)
                throw new Exception(
                    $"Cannot make enum value \"{flag.Name}\" as the amount of arguments ({args.Count}) "
                        + "does not match the amount of parameters ({unionEnumFlag.UnionTypes.Count}) required."
                );

            int index = 0;

            foreach (var val in args)
            {
                if (!val.Type.Equals(args[index].Type))
                    throw new Exception();
                index++;
            }
        }

        var argsFinal = new LLVMValueRef[args.Count + 1];
        argsFinal[0] = LLVMValueRef.CreateConstInt(FlagType.LLVMType, flag.Value);
        args.ToArray().AsLLVMValues().CopyTo(argsFinal, 1);

        return Value.Create(
            _compiler,
            this,
            LLVMValueRef.CreateConstNamedStruct(LLVMType, argsFinal)
        );
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
}
