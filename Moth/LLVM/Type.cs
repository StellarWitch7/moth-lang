using LLVMSharp.Interop;
using Moth.LLVM.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class Type
{
    public LLVMTypeRef LLVMType { get; set; }
    public Class Class { get; set; }
    public string Is { get; init; }

    public Type(LLVMTypeRef lLVMType, Class @class)
    {
        Is = "Type";
        LLVMType = lLVMType;
        Class = @class;
    }

    public override string ToString()
    {
        return Class.Name;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Type type)
        {
            return false;
        }

        if (LLVMType.Kind != type.LLVMType.Kind)
        {
            return false;
        }

        if (Class.Name != type.Class.Name)
        {
            return false;
        }

        if (Is != type.Is)
        {
            return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        return Is.GetHashCode() * Class.Name.GetHashCode() * (int)LLVMType.Kind;
    }
}

public class BasedType : Type
{
    public readonly Type BaseType;

    public BasedType(Type baseType, LLVMTypeRef lLVMType, Class classOfType) : base(lLVMType, classOfType)
    {
        Is = "BasedType";
        BaseType = baseType;
    }

    public uint GetDepth()
    {
        var type = BaseType;
        uint depth = 0;

        while (type != null)
        {
            depth++;
            type = type is BasedType bType ? bType.BaseType : null;
        }

        return depth;
    }

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder(Class.Name);
        var type = BaseType;

        while (type != null)
        {
            builder.Append('*');
            type = type is BasedType bType ? bType.BaseType : null;
        }

        return builder.ToString();
    }

    public override bool Equals(object? obj)
    {
        if (!base.Equals(obj))
        {
            return false;
        }

        if (obj is BasedType bType)
        {
            return BaseType.Equals(bType.BaseType);
        }
        else
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        return base.GetHashCode() * BaseType.GetHashCode();
    }
}

public sealed class RefType : BasedType
{
    public RefType(Type baseType, LLVMTypeRef lLVMType, Class classOfType) : base(baseType, lLVMType, classOfType)
    {
        Is = "RefType";
    }
}

public sealed class PtrType : BasedType
{
    public PtrType(Type baseType, LLVMTypeRef lLVMType, Class classOfType) : base(baseType, lLVMType, classOfType)
    {
        Is = "PtrType";
    }
}