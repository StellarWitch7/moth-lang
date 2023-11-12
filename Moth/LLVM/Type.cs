using Moth.AST.Node;
using Moth.LLVM.Data;

namespace Moth.LLVM;

public enum TypeKind
{
    Class,
    Function,
    Pointer,
    Reference,
}

public abstract class Type
{
    public readonly LLVMTypeRef LLVMType;
    public readonly TypeKind Kind;


    public Type(LLVMTypeRef llvmType, TypeKind kind)
    {
        LLVMType = llvmType;
        Kind = kind;
    }

    public abstract override string ToString();
    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();
}

public class ClassType : Type
{
    public readonly Class Class;

    public ClassType(LLVMTypeRef llvmType, Class @class) : this(llvmType, @class, TypeKind.Class) { }

    public ClassType(LLVMTypeRef llvmType, Class @class, TypeKind kind) : base(llvmType, kind) => Class = @class;

    public override string ToString() => Class.Name;

    public override bool Equals(object? obj)
        => obj is ClassType type
            && Class != null
            && type.Class != null
            && LLVMType.Kind == type.LLVMType.Kind
            && Kind == type.Kind
            && Class.Name == type.Class.Name;

    public override int GetHashCode() => Kind.GetHashCode() * Class.Name.GetHashCode() * (int)LLVMType.Kind;
}

public class BasedType : Type
{
    public readonly Type BaseType;

    public BasedType(Type baseType, TypeKind kind)
        : base(LLVMTypeRef.CreatePointer(baseType.LLVMType, 0), kind) => BaseType = baseType;

    public uint GetDepth()
    {
        Type? type = BaseType;
        uint depth = 0;

        while (type != null)
        {
            depth++;
            type = type is BasedType bType ? bType.BaseType : null;
        }

        return depth;
    }

    public override string ToString() => BaseType.ToString() + "*";

    public override bool Equals(object? obj) => obj is BasedType bType && BaseType.Equals(bType.BaseType);

    public override int GetHashCode() => BaseType.GetHashCode();
}

public sealed class RefType : BasedType
{
    public RefType(Type baseType)
        : base(baseType, TypeKind.Reference) { }
}

public sealed class PtrType : BasedType
{
    public PtrType(Type baseType)
        : base(baseType, TypeKind.Pointer) { }
}

public abstract class FuncType : Type
{
    public readonly string Name;
    public Type ReturnType { get; }
    public Type[] ParameterTypes { get; }

    public FuncType(string name, Type retType, Type[] paramTypes)
        : base(LLVMTypeRef.CreateFunction(retType.LLVMType, paramTypes.AsLLVMTypes()), TypeKind.Function)
    {
        Name = name;
        ReturnType = retType;
        ParameterTypes = paramTypes;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not FuncType fnType)
        {
            return false;
        }

        if (!ReturnType.Equals(fnType.ReturnType))
        {
            return false;
        }

        if (ParameterTypes.Length != fnType.ParameterTypes.Length)
        {
            return false;
        }

        uint index = 0;

        foreach (Type param in ParameterTypes)
        {
            if (!param.Equals(fnType.ParameterTypes[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    public override int GetHashCode() => Name.GetHashCode() * ParameterTypes.GetHashes();

    public abstract Value Call(LLVMCompiler compiler, LLVMValueRef func, Value[] args);
}

public class LLVMFunction : FuncType
{
    public Scope? OpeningScope { get; }

    public readonly IReadOnlyList<Parameter> Params;
    public readonly bool IsVariadic;

    public LLVMFunction(string name, Type retType, Type[] paramTypes,
        IReadOnlyList<Parameter> @params, bool isVariadic)
        : base(name, retType, paramTypes)
    {
        Params = @params;
        IsVariadic = isVariadic;
    }

    public Class? OwnerClass
    {
        get
        {
            return this is DefinedFunction defFunc
                ? defFunc.OwnerClass
                : null;
        }
    }

    public override Value Call(LLVMCompiler compiler, LLVMValueRef func, Value[] args)
        => new Value(ReturnType, compiler.Builder.BuildCall2(LLVMType,
            func,
            args.AsLLVMValues(),
            ReturnType.LLVMType.Kind != LLVMTypeKind.LLVMVoidTypeKind
                ? Name
                : ""));

    public override string ToString() => throw new NotImplementedException();
}

public sealed class DefinedFunction : LLVMFunction
{
    public new Class? OwnerClass { get; set; }

    public readonly PrivacyType Privacy;

    public DefinedFunction(string name, Type retType, Type[] paramTypes,
        PrivacyType privacy, Class? ownerClass,
        IReadOnlyList<Parameter> @params, bool isVariadic)
        : base(name, retType, paramTypes, @params, isVariadic)
    {
        Privacy = privacy;
        OwnerClass = ownerClass;
    }
}

public sealed class LocalFunction : LLVMFunction
{
    public LocalFunction(Type retType, Type[] paramTypes, IReadOnlyList<Parameter> @params)
        : base("localfunc", retType, paramTypes, @params, false) { }
}

public abstract class IntrinsicFunction : FuncType
{
    protected IntrinsicFunction(string name, Type retType, Type[] paramTypes)
        : base(name, retType, paramTypes) { }

    protected virtual LLVMValueRef GenerateLLVMData()
        => throw new NotImplementedException("This function does not support LLVM data generation.");
}