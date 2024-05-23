using Moth.AST.Node;
using Moth.LLVM.Data;
using Void = Moth.LLVM.Data.Void;

namespace Moth.LLVM;

public static class Primitives
{
    public static readonly Null Null = new Null();
    public static readonly Void Void = new Void();
    
    public static readonly UnsignedInt Bool = new UnsignedInt(Reserved.Bool, LLVMTypeRef.Int1, 1);
    public static readonly UnsignedInt UInt8 = new UnsignedInt(Reserved.UInt8, LLVMTypeRef.Int8, 8);
    public static readonly UnsignedInt UInt16 = new UnsignedInt(Reserved.UInt16, LLVMTypeRef.Int16, 16);
    public static readonly UnsignedInt UInt32 = new UnsignedInt(Reserved.UInt32, LLVMTypeRef.Int32, 32);
    public static readonly UnsignedInt UInt64 = new UnsignedInt(Reserved.UInt64, LLVMTypeRef.Int64, 64);
    public static readonly SignedInt UInt128 = new SignedInt(Reserved.UInt128, LLVMTypeRef.CreateInt(128), 128);
    
    public static readonly SignedInt Int8 = new SignedInt(Reserved.Int8, LLVMTypeRef.Int8, 8);
    public static readonly SignedInt Int16 = new SignedInt(Reserved.Int16, LLVMTypeRef.Int16, 16);
    public static readonly SignedInt Int32 = new SignedInt(Reserved.Int32, LLVMTypeRef.Int32, 32);
    public static readonly SignedInt Int64 = new SignedInt(Reserved.Int64, LLVMTypeRef.Int64, 64);
    public static readonly SignedInt Int128 = new SignedInt(Reserved.Int128, LLVMTypeRef.CreateInt(128), 128);

    public static readonly Float Float16 = new Float(Reserved.Float16, LLVMTypeRef.Half, 16);
    public static readonly Float Float32 = new Float(Reserved.Float32, LLVMTypeRef.Float, 32);
    public static readonly Float Float64 = new Float(Reserved.Float64, LLVMTypeRef.Double, 64);
}
