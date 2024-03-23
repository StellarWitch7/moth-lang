using Moth.AST.Node;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Moth.LLVM.Data;

public class PrimitiveType : Struct
{
    private uint _bitlength;
    
    internal PrimitiveType(string name, LLVMTypeRef llvmType, uint bitlength)
        : base(null, name, llvmType, PrivacyType.Public)
    {
        _bitlength = bitlength;
    }

    public override string FullName
    {
        get
        {
            return $"#{Name}";
        }
    }

    public override uint Bits
    {
        get
        {
            return _bitlength;
        }
    }
}

public sealed class ArrType : PrimitiveType
{
    public Type ElementType { get; }
    
    public ArrType(LLVMCompiler compiler, Type elementType)
        : base($"[{elementType}]",
            compiler.Context.GetStructType(new []
            {
                new PtrType(elementType).LLVMType,
                LLVMTypeRef.Int32
                
            }, false), 64)
    {
        Fields.Add("Length", new Field("Length", 1, Primitives.UInt32, PrivacyType.Public));
        Methods.TryAdd(Reserved.Indexer, new OverloadList(Reserved.Indexer));
        Methods[Reserved.Indexer].Add(new ArrayIndexerFunction(compiler, this, elementType));
        ElementType = elementType;
    }

    public override string ToString() => $"#[{ElementType}]";

    public override bool Equals(object? obj)
    {
        if (obj is not ArrType arrType)
        {
            return false;
        }

        if (!ElementType.Equals(arrType.ElementType))
        {
            return false;
        }

        return base.Equals(obj);
    }

    public override int GetHashCode() => base.GetHashCode() + ElementType.GetHashCode();
}

public class Void : PrimitiveType
{
    public Void() : base(Reserved.Void, LLVMTypeRef.Void, 0) { }
    
    public class ImplicitConversionTable : LLVM.ImplicitConversionTable
    {
        public ImplicitConversionTable() { }

        public override bool Contains(Type key)
        {
            return key is PtrType;
        }
        
        public override bool TryGetValue(Type key, [MaybeNullWhen(false)] out Func<LLVMCompiler, Value, Value> value)
        {
            if (key is PtrType ptrType)
            {
                value = (compiler, prev) =>
                {
                    return new Pointer(ptrType, prev.LLVMValue);
                };
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }
    }
}

public class Null : PrimitiveType
{
    public Null() : base(Reserved.Null, LLVMTypeRef.Int8, 8) { }

    public override ImplicitConversionTable GetImplicitConversions() => new ImplicitConversionTable();

    public class ImplicitConversionTable : LLVM.ImplicitConversionTable
    {
        public ImplicitConversionTable() { }

        public override bool Contains(Type key)
        {
            return true;
        }
        
        public override bool TryGetValue(Type key, [MaybeNullWhen(false)] out Func<LLVMCompiler, Value, Value> value)
        {
            if (key is PtrType)
            {
                value = (compiler, prev) =>
                {
                    return Value.Create(key, LLVMValueRef.CreateConstPointerNull(key.LLVMType));
                };
                return true;
            }
            else
            {
                value = (compiler, prev) =>
                {
                    return Value.Create(key, LLVMValueRef.CreateConstNull(key.LLVMType));
                };
                return true;
            }
        }
    }
}