using Moth.AST.Node;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Moth.LLVM.Data;

public abstract class PrimitiveType : Type
{
    private uint _bitlength;
    private bool _methodsGenerated = false;
    
    protected PrimitiveType(string name, LLVMTypeRef llvmType, uint bitlength)
        : base(null, null, name, llvmType, new Dictionary<string, IAttribute>(), PrivacyType.Pub, false)
    {
        _bitlength = bitlength;
    }

    public override string FullName
    {
        get
        {
            return $"root#{Name}";
        }
    }

    public override uint Bits
    {
        get
        {
            return _bitlength;
        }
    }

    public override Dictionary<string, OverloadList> Methods
    {
        get
        {
            if (!_methodsGenerated)
            {
                foreach (var kv in GenerateDefaultMethods())
                {
                    base.Methods.Add(kv.Key, kv.Value);
                }

                _methodsGenerated = true;
            }
            
            return base.Methods;
        }
    }
    
    protected OverloadList InitOperatorList(Dictionary<string, OverloadList> dict, OperationType opType)
    {
        var opName = Utils.ExpandOpName(Utils.OpTypeToString(opType));
        var op = new OverloadList(opName);
        
        dict.Add(opName, op);
        return op;
    }
    
    protected abstract Dictionary<string, OverloadList> GenerateDefaultMethods();
}

public sealed class ArrType : PrimitiveType
{
    public InternalType ElementType { get; }

    private LLVMCompiler _compiler;
    
    public ArrType(LLVMCompiler compiler, InternalType elementType)
        : base($"[{elementType}]",
            compiler.Context.GetStructType(new []
            {
                new PtrType(elementType).LLVMType,
                LLVMTypeRef.Int32
                
            }, false), 64)
    {
        _compiler = compiler;
        Fields.Add("Length", new Field("Length", 1, Primitives.UInt32, PrivacyType.Pub));
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

    protected override Dictionary<string, OverloadList> GenerateDefaultMethods()
    {
        var dict = new Dictionary<string, OverloadList>();
        var indexer = new OverloadList(Reserved.Indexer);
        
        indexer.Add(new ArrayIndexerFunction(_compiler, this, ElementType));
        dict.Add(Reserved.Indexer, indexer);

        return dict;
    }
}

public class Void : PrimitiveType
{
    public Void() : base(Reserved.Void, LLVMTypeRef.Void, 0) { }

    protected override Dictionary<string, OverloadList> GenerateDefaultMethods()
    {
        return new Dictionary<string, OverloadList>();
    }

    public class ImplicitConversionTable : LLVM.ImplicitConversionTable
    {
        public override bool Contains(InternalType key)
        {
            return key is PtrType;
        }
        
        public override bool TryGetValue(InternalType key, [MaybeNullWhen(false)] out Func<LLVMCompiler, Value, Value> value)
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

    protected override Dictionary<string, OverloadList> GenerateDefaultMethods()
    {
        return new Dictionary<string, OverloadList>();
    }
    
    public class ImplicitConversionTable : LLVM.ImplicitConversionTable
    {
        public override bool Contains(InternalType key)
        {
            return true;
        }
        
        public override bool TryGetValue(InternalType key, [MaybeNullWhen(false)] out Func<LLVMCompiler, Value, Value> value)
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