using Moth.AST.Node;
using System.Reflection;
using System.Reflection.Emit;

namespace Moth.LLVM.Data;

public abstract class Int : PrimitiveStructDecl
{
    protected Int(string name, LLVMTypeRef llvmType, uint bitlength) : base(name, llvmType, bitlength) { }
    
    public override ImplicitConversionTable GetImplicitConversions()
    {
        if (_internalImplicits == null)
        {
            _internalImplicits = GenerateImplicitConversions();
        }
        
        return _internalImplicits;
    }

    protected override Dictionary<string, OverloadList> GenerateDefaultMethods()
    {
        var dict = new Dictionary<string, OverloadList>();

        AddOperator(InitOperatorList(dict, OperationType.Addition), typeof(Addition));
        AddOperator(InitOperatorList(dict, OperationType.Subtraction), typeof(Subtraction));
        AddOperator(InitOperatorList(dict, OperationType.Multiplication), typeof(Multiplication));
        AddOperator(InitOperatorList(dict, OperationType.Division), typeof(Division));
        //TODO //AddOperator(InitOperatorList(dict, OperationType.Exponential), typeof(Exponential));
        AddOperator(InitOperatorList(dict, OperationType.Modulus), typeof(Modulus));
        AddOperator(InitOperatorList(dict, OperationType.LesserThan), typeof(LesserThan));
        AddOperator(InitOperatorList(dict, OperationType.LesserThanOrEqual), typeof(LesserThanOrEqual));
        AddOperator(InitOperatorList(dict, OperationType.GreaterThan), typeof(GreaterThan));
        AddOperator(InitOperatorList(dict, OperationType.GreaterThanOrEqual), typeof(GreaterThanOrEqual));
        AddOperator(InitOperatorList(dict, OperationType.Equal), typeof(Equal));

        return dict;
    }
    
    protected void AddOperator(OverloadList funcList, SystemType operation)
    {
        bool retBool = retBool = funcList.Name == Utils.ExpandOpName("==")
            || funcList.Name == Utils.ExpandOpName("<")
            || funcList.Name == Utils.ExpandOpName("<=")
            || funcList.Name == Utils.ExpandOpName(">")
            || funcList.Name == Utils.ExpandOpName(">=");
        var e = new Exception("Internal error: function constructor didn't return function.");
        var ctor = operation.GetConstructor(new SystemType[]
        {
            typeof(PrimitiveStructDecl),
            typeof(PrimitiveStructDecl),
            typeof(PrimitiveStructDecl)
        });

        if (ctor is null) throw new Exception("Internal error: function constructor cannot be found.");
        
        {
            funcList.Add(ctor.Invoke(new object[]{ retBool ? Primitives.Bool : this, this, this }) is Function func
                ? func
                : throw e);
        }

        {
            funcList.Add(ctor.Invoke(new object[]{ retBool ? Primitives.Bool : this, this, new AbstractInt(0) }) is Function func
                ? func
                : throw e);
        }

        if (funcList.Name == Utils.ExpandOpName("=="))
        {
            funcList.Add(ctor.Invoke(new object[]{ retBool ? Primitives.Bool : this, this, Primitives.Null }) is Function func
                ? func
                : throw e);
        }

        if (this is SignedInt)
        {
            if (Bits < Primitives.Int8.Bits) funcList.Add(ctor.Invoke(new object[]
                { retBool ? Primitives.Bool : Primitives.Int8, this, Primitives.Int8 }) is Function func ? func : throw e);
            if (Bits < Primitives.Int16.Bits) funcList.Add(ctor.Invoke(new object[]
                { retBool ? Primitives.Bool : Primitives.Int16, this, Primitives.Int16 }) is Function func ? func : throw e);
            if (Bits < Primitives.Int32.Bits) funcList.Add(ctor.Invoke(new object[]
                { retBool ? Primitives.Bool : Primitives.Int32, this, Primitives.Int32 }) is Function func ? func : throw e);
            if (Bits < Primitives.Int64.Bits) funcList.Add(ctor.Invoke(new object[]
                { retBool ? Primitives.Bool : Primitives.Int64, this, Primitives.Int64 }) is Function func ? func : throw e);
        
            if (Bits > Primitives.Int8.Bits) funcList.Add(ctor.Invoke(new object[]
                { retBool ? Primitives.Bool : this, this, Primitives.Int8 }) is Function func ? func : throw e);
            if (Bits > Primitives.Int16.Bits) funcList.Add(ctor.Invoke(new object[]
                { retBool ? Primitives.Bool : this, this, Primitives.Int16 }) is Function func ? func : throw e);
            if (Bits > Primitives.Int32.Bits) funcList.Add(ctor.Invoke(new object[]
                { retBool ? Primitives.Bool : this, this, Primitives.Int32 }) is Function func ? func : throw e);
            if (Bits > Primitives.Int64.Bits) funcList.Add(ctor.Invoke(new object[]
                { retBool ? Primitives.Bool : this, this, Primitives.Int64 }) is Function func ? func : throw e);
        }
        else
        {
            if (Bits < Primitives.UInt8.Bits) funcList.Add(ctor.Invoke(new object[]
                { retBool ? Primitives.Bool : Primitives.UInt8, this, Primitives.UInt8 }) is Function func ? func : throw e);
            if (Bits < Primitives.UInt16.Bits) funcList.Add(ctor.Invoke(new object[]
                { retBool ? Primitives.Bool : Primitives.UInt16, this, Primitives.UInt16 }) is Function func ? func : throw e);
            if (Bits < Primitives.UInt32.Bits) funcList.Add(ctor.Invoke(new object[]
                { retBool ? Primitives.Bool : Primitives.UInt32, this, Primitives.UInt32 }) is Function func ? func : throw e);
            if (Bits < Primitives.UInt64.Bits) funcList.Add(ctor.Invoke(new object[]
                { retBool ? Primitives.Bool : Primitives.UInt64, this, Primitives.UInt64 }) is Function func ? func : throw e);
        
            if (Bits > Primitives.UInt8.Bits) funcList.Add(ctor.Invoke(new object[]
                { retBool ? Primitives.Bool : this, this, Primitives.UInt8 }) is Function func ? func : throw e);
            if (Bits > Primitives.UInt16.Bits) funcList.Add(ctor.Invoke(new object[]
                { retBool ? Primitives.Bool : this, this, Primitives.UInt16 }) is Function func ? func : throw e);
            if (Bits > Primitives.UInt32.Bits) funcList.Add(ctor.Invoke(new object[]
                { retBool ? Primitives.Bool : this, this, Primitives.UInt32 }) is Function func ? func : throw e);
            if (Bits > Primitives.UInt64.Bits) funcList.Add(ctor.Invoke(new object[]
                { retBool ? Primitives.Bool : this, this, Primitives.UInt64 }) is Function func ? func : throw e);
        }
    }
    
    protected abstract ImplicitConversionTable GenerateImplicitConversions();
}

public sealed class SignedInt : Int
{
    public SignedInt(string name, LLVMTypeRef llvmType, uint bitlength) : base(name, llvmType, bitlength) { }

    protected override ImplicitConversionTable GenerateImplicitConversions()
    {
        var dict = new ImplicitConversionTable();
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
    
    protected override ImplicitConversionTable GenerateImplicitConversions()
    {
        var dict = new ImplicitConversionTable();
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

