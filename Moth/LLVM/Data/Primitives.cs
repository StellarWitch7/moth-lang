using Moth.AST.Node;

namespace Moth.LLVM.Data;

public static class Primitives
{
    public static readonly Struct Void = new PrimitiveType(Reserved.Void, LLVMTypeRef.Void);
    
    public static readonly Float Float16 = new Float(Reserved.Float16, LLVMTypeRef.Half);
    public static readonly Float Float32 = new Float(Reserved.Float32, LLVMTypeRef.Float);
    public static readonly Float Float64 = new Float(Reserved.Float64, LLVMTypeRef.Double);
    
    public static readonly SignedInt Int8 = new SignedInt(Reserved.SignedInt8, LLVMTypeRef.Int8);
    public static readonly SignedInt Int16 = new SignedInt(Reserved.SignedInt16, LLVMTypeRef.Int16);
    public static readonly SignedInt Int32 = new SignedInt(Reserved.SignedInt32, LLVMTypeRef.Int32);
    public static readonly SignedInt Int64 = new SignedInt(Reserved.SignedInt64, LLVMTypeRef.Int64);
    
    public static readonly UnsignedInt Bool = new UnsignedInt(Reserved.Bool, LLVMTypeRef.Int1);
    public static readonly UnsignedInt Char = new UnsignedInt(Reserved.Char, LLVMTypeRef.Int8);
    public static readonly UnsignedInt UInt8 = new UnsignedInt(Reserved.UnsignedInt8, LLVMTypeRef.Int8);
    public static readonly UnsignedInt UInt16 = new UnsignedInt(Reserved.UnsignedInt16, LLVMTypeRef.Int16);
    public static readonly UnsignedInt UInt32 = new UnsignedInt(Reserved.UnsignedInt32, LLVMTypeRef.Int32);
    public static readonly UnsignedInt UInt64 = new UnsignedInt(Reserved.UnsignedInt64, LLVMTypeRef.Int64);
}
