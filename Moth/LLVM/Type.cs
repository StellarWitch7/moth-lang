using LLVMSharp.Interop;
using Moth.LLVM.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class Type
{
    public LLVMTypeRef LLVMType { get; set; }
    public Class Class { get; set; }

    public Type(LLVMTypeRef lLVMType, Class @class)
    {
        LLVMType = lLVMType;
        Class = @class;
    }
}

public sealed class RefType : Type
{
    public readonly Type BaseType;

    public RefType(Type baseType, LLVMTypeRef lLVMType, Class classOfType) : base(lLVMType, classOfType)
    {
        BaseType = baseType;
    }
}

public sealed class PtrType : Type
{
    public readonly Type BaseType;

    public PtrType(Type baseType, LLVMTypeRef lLVMType, Class classOfType) : base(lLVMType, classOfType)
    {
        BaseType = baseType;
    }
}
