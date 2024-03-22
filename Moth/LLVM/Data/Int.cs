using Moth.AST.Node;

namespace Moth.LLVM.Data;

public abstract class Int : PrimitiveType
{
    protected Dictionary<Type, Func<LLVMCompiler, Value, Value>> internalImplicits = null;
    
    protected Int(string name, LLVMTypeRef llvmType, uint bitlength) : base(name, llvmType, bitlength) { }
    
    public override Dictionary<Type, Func<LLVMCompiler, Value, Value>> GetImplicitConversions()
    {
        if (internalImplicits == null)
        {
            internalImplicits = GenerateImplicitConversions();
        }
        
        return internalImplicits;
    }

    protected abstract Dictionary<Type, Func<LLVMCompiler, Value, Value>> GenerateImplicitConversions();
}

public sealed class SignedInt : Int
{
    public SignedInt(string name, LLVMTypeRef llvmType, uint bitlength) : base(name, llvmType, bitlength) { }

    protected override Dictionary<Type, Func<LLVMCompiler, Value, Value>> GenerateImplicitConversions()
    {
        var dict = new Dictionary<Type, Func<LLVMCompiler, Value, Value>>();

        var create = (Int destType) =>
        {
            dict.Add(destType, (compiler, prev) =>
            {
                return Value.Create(destType, compiler.Builder.BuildIntCast(prev.LLVMValue, destType.LLVMType));
            });
        };
        
        if (Primitives.Int64.Bits > Bits)
        {
            create(Primitives.Int64);
        }

        if (Primitives.Int32.Bits > Bits)
        {
            create(Primitives.Int32);
        }

        if (Primitives.Int16.Bits > Bits)
        {
            create(Primitives.Int16);
        }

        return dict;
    }
}

public sealed class UnsignedInt : Int
{
    public UnsignedInt(string name, LLVMTypeRef llvmType, uint bitlength) : base(name, llvmType, bitlength) { }
    
    protected override Dictionary<Type, Func<LLVMCompiler, Value, Value>> GenerateImplicitConversions()
    {
        var dict = new Dictionary<Type, Func<LLVMCompiler, Value, Value>>();
        var create = (Int destType) =>
        {
            dict.Add(destType, (compiler, prev) =>
            {
                LLVMValueRef newVal;
                
                if (Bits == 1)
                {
                    newVal = compiler.Builder.BuildZExt(prev.LLVMValue, destType.LLVMType);
                }
                else
                {
                    newVal = compiler.Builder.BuildIntCast(prev.LLVMValue, destType.LLVMType);
                }
                
                return Value.Create(destType, newVal);
            });
        };
        
        if (Primitives.Int64.Bits > Bits)
        {
            create(Primitives.Int64);
        }

        if (Primitives.Int32.Bits > Bits)
        {
            create(Primitives.Int32);
        }

        if (Primitives.Int16.Bits > Bits)
        {
            create(Primitives.Int16);
        }
        
        if (Primitives.Int8.Bits > Bits)
        {
            create(Primitives.Int8);
        }
        
        if (Primitives.UInt64.Bits > Bits)
        {
            create(Primitives.UInt64);
        }

        if (Primitives.UInt32.Bits > Bits)
        {
            create(Primitives.UInt32);
        }

        if (Primitives.UInt16.Bits > Bits)
        {
            create(Primitives.UInt16);
        }
        
        if (Primitives.UInt8.Bits > Bits)
        {
            create(Primitives.UInt8);
        }

        return dict;
    }
}

