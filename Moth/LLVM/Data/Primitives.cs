using Moth.AST.Node;

namespace Moth.LLVM.Data;

public static class Primitives
{
    public static readonly Class Void = new Class(null, Reserved.Void, LLVMTypeRef.Void, PrivacyType.Public);
    
    public static readonly SignedInt Int8 = new SignedInt(Reserved.SignedInt8, LLVMTypeRef.Int8, PrivacyType.Public);
    public static readonly SignedInt Int16 = new SignedInt(Reserved.SignedInt16, LLVMTypeRef.Int16, PrivacyType.Public);
    public static readonly SignedInt Int32 = new SignedInt(Reserved.SignedInt32, LLVMTypeRef.Int32, PrivacyType.Public);
    public static readonly SignedInt Int64 = new SignedInt(Reserved.SignedInt64, LLVMTypeRef.Int64, PrivacyType.Public);
    
    public static readonly UnsignedInt Bool = new UnsignedInt(Reserved.Bool, LLVMTypeRef.Int1, PrivacyType.Public);
    public static readonly UnsignedInt Char = new UnsignedInt(Reserved.Char, LLVMTypeRef.Int8, PrivacyType.Public);
    public static readonly UnsignedInt UInt8 = new UnsignedInt(Reserved.UnsignedInt8, LLVMTypeRef.Int8, PrivacyType.Public);
    public static readonly UnsignedInt UInt16 = new UnsignedInt(Reserved.UnsignedInt16, LLVMTypeRef.Int16, PrivacyType.Public);
    public static readonly UnsignedInt UInt32 = new UnsignedInt(Reserved.UnsignedInt32, LLVMTypeRef.Int32, PrivacyType.Public);
    public static readonly UnsignedInt UInt64 = new UnsignedInt(Reserved.UnsignedInt64, LLVMTypeRef.Int64, PrivacyType.Public);
}
