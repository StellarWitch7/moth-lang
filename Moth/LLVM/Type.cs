using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class Type
{
    public LLVMTypeRef LLVMType { get; set; }

    public Type(LLVMTypeRef lLVMType)
    {
        LLVMType = lLVMType;
    }
}

public sealed class RefType : Type
{
    public readonly Type BaseType;

    public RefType(Type baseType, LLVMTypeRef lLVMType) : base(lLVMType)
    {
        BaseType = baseType;
    }
}

public sealed class PtrType : Type
{
    public readonly Type BaseType;

    public PtrType(Type baseType, LLVMTypeRef lLVMType) : base(lLVMType)
    {
        BaseType = baseType;
    }
}
