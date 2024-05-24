using Moth.AST.Node;

namespace Moth.LLVM.Data;

public abstract class Int : PrimitiveStructDecl
{
    protected Int(LLVMCompiler compiler, string name, LLVMTypeRef llvmType, uint bitlength)
        : base(compiler, name, llvmType, bitlength) { }

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
        AddOperator(
            InitOperatorList(dict, OperationType.LesserThanOrEqual),
            typeof(LesserThanOrEqual)
        );
        AddOperator(InitOperatorList(dict, OperationType.GreaterThan), typeof(GreaterThan));
        AddOperator(
            InitOperatorList(dict, OperationType.GreaterThanOrEqual),
            typeof(GreaterThanOrEqual)
        );
        AddOperator(InitOperatorList(dict, OperationType.Equal), typeof(Equal));

        return dict;
    }

    protected void AddOperator(OverloadList funcList, SystemType operation)
    {
        bool retBool = retBool =
            funcList.Name == Utils.ExpandOpName("==")
            || funcList.Name == Utils.ExpandOpName("<")
            || funcList.Name == Utils.ExpandOpName("<=")
            || funcList.Name == Utils.ExpandOpName(">")
            || funcList.Name == Utils.ExpandOpName(">=");
        var e = new Exception("Internal error: function constructor didn't return function.");
        var ctor = operation.GetConstructor(
            new SystemType[]
            {
                typeof(PrimitiveStructDecl),
                typeof(PrimitiveStructDecl),
                typeof(PrimitiveStructDecl)
            }
        );

        if (ctor is null)
            throw new Exception("Internal error: function constructor cannot be found.");

        {
            funcList.Add(
                ctor.Invoke(new object[] { retBool ? _compiler.Bool : this, this, this })
                    is Function func
                    ? func
                    : throw e
            );
        }

        {
            funcList.Add(
                ctor.Invoke(
                    new object[]
                    {
                        retBool ? _compiler.Bool : this,
                        this,
                        new AbstractInt(_compiler, 0)
                    }
                )
                    is Function func
                    ? func
                    : throw e
            );
        }

        if (funcList.Name == Utils.ExpandOpName("=="))
        {
            funcList.Add(
                ctor.Invoke(new object[] { retBool ? _compiler.Bool : this, this, _compiler.Null })
                    is Function func
                    ? func
                    : throw e
            );
        }

        if (this is SignedInt)
        {
            if (Bits < _compiler.Int8.Bits)
                funcList.Add(
                    ctor.Invoke(
                        new object[]
                        {
                            retBool ? _compiler.Bool : _compiler.Int8,
                            this,
                            _compiler.Int8
                        }
                    )
                        is Function func
                        ? func
                        : throw e
                );
            if (Bits < _compiler.Int16.Bits)
                funcList.Add(
                    ctor.Invoke(
                        new object[]
                        {
                            retBool ? _compiler.Bool : _compiler.Int16,
                            this,
                            _compiler.Int16
                        }
                    )
                        is Function func
                        ? func
                        : throw e
                );
            if (Bits < _compiler.Int32.Bits)
                funcList.Add(
                    ctor.Invoke(
                        new object[]
                        {
                            retBool ? _compiler.Bool : _compiler.Int32,
                            this,
                            _compiler.Int32
                        }
                    )
                        is Function func
                        ? func
                        : throw e
                );
            if (Bits < _compiler.Int64.Bits)
                funcList.Add(
                    ctor.Invoke(
                        new object[]
                        {
                            retBool ? _compiler.Bool : _compiler.Int64,
                            this,
                            _compiler.Int64
                        }
                    )
                        is Function func
                        ? func
                        : throw e
                );

            if (Bits > _compiler.Int8.Bits)
                funcList.Add(
                    ctor.Invoke(
                        new object[] { retBool ? _compiler.Bool : this, this, _compiler.Int8 }
                    )
                        is Function func
                        ? func
                        : throw e
                );
            if (Bits > _compiler.Int16.Bits)
                funcList.Add(
                    ctor.Invoke(
                        new object[] { retBool ? _compiler.Bool : this, this, _compiler.Int16 }
                    )
                        is Function func
                        ? func
                        : throw e
                );
            if (Bits > _compiler.Int32.Bits)
                funcList.Add(
                    ctor.Invoke(
                        new object[] { retBool ? _compiler.Bool : this, this, _compiler.Int32 }
                    )
                        is Function func
                        ? func
                        : throw e
                );
            if (Bits > _compiler.Int64.Bits)
                funcList.Add(
                    ctor.Invoke(
                        new object[] { retBool ? _compiler.Bool : this, this, _compiler.Int64 }
                    )
                        is Function func
                        ? func
                        : throw e
                );
        }
        else
        {
            if (Bits < _compiler.UInt8.Bits)
                funcList.Add(
                    ctor.Invoke(
                        new object[]
                        {
                            retBool ? _compiler.Bool : _compiler.UInt8,
                            this,
                            _compiler.UInt8
                        }
                    )
                        is Function func
                        ? func
                        : throw e
                );
            if (Bits < _compiler.UInt16.Bits)
                funcList.Add(
                    ctor.Invoke(
                        new object[]
                        {
                            retBool ? _compiler.Bool : _compiler.UInt16,
                            this,
                            _compiler.UInt16
                        }
                    )
                        is Function func
                        ? func
                        : throw e
                );
            if (Bits < _compiler.UInt32.Bits)
                funcList.Add(
                    ctor.Invoke(
                        new object[]
                        {
                            retBool ? _compiler.Bool : _compiler.UInt32,
                            this,
                            _compiler.UInt32
                        }
                    )
                        is Function func
                        ? func
                        : throw e
                );
            if (Bits < _compiler.UInt64.Bits)
                funcList.Add(
                    ctor.Invoke(
                        new object[]
                        {
                            retBool ? _compiler.Bool : _compiler.UInt64,
                            this,
                            _compiler.UInt64
                        }
                    )
                        is Function func
                        ? func
                        : throw e
                );

            if (Bits > _compiler.UInt8.Bits)
                funcList.Add(
                    ctor.Invoke(
                        new object[] { retBool ? _compiler.Bool : this, this, _compiler.UInt8 }
                    )
                        is Function func
                        ? func
                        : throw e
                );
            if (Bits > _compiler.UInt16.Bits)
                funcList.Add(
                    ctor.Invoke(
                        new object[] { retBool ? _compiler.Bool : this, this, _compiler.UInt16 }
                    )
                        is Function func
                        ? func
                        : throw e
                );
            if (Bits > _compiler.UInt32.Bits)
                funcList.Add(
                    ctor.Invoke(
                        new object[] { retBool ? _compiler.Bool : this, this, _compiler.UInt32 }
                    )
                        is Function func
                        ? func
                        : throw e
                );
            if (Bits > _compiler.UInt64.Bits)
                funcList.Add(
                    ctor.Invoke(
                        new object[] { retBool ? _compiler.Bool : this, this, _compiler.UInt64 }
                    )
                        is Function func
                        ? func
                        : throw e
                );
        }
    }

    protected abstract ImplicitConversionTable GenerateImplicitConversions();
}

public sealed class SignedInt : Int
{
    public SignedInt(LLVMCompiler compiler, string name, LLVMTypeRef llvmType, uint bitlength)
        : base(compiler, name, llvmType, bitlength) { }

    protected override ImplicitConversionTable GenerateImplicitConversions()
    {
        var dict = new ImplicitConversionTable(_compiler);
        var create = (Int destType) =>
        {
            dict.Add(
                destType,
                (prev) =>
                {
                    return Value.Create(
                        _compiler,
                        destType,
                        _compiler.Builder.BuildIntCast(prev.LLVMValue, destType.LLVMType)
                    );
                }
            );
        };

        if (_compiler.Int64.Bits > Bits)
        {
            create(_compiler.Int64);
        }

        if (_compiler.Int32.Bits > Bits)
        {
            create(_compiler.Int32);
        }

        if (_compiler.Int16.Bits > Bits)
        {
            create(_compiler.Int16);
        }

        return dict;
    }
}

public sealed class UnsignedInt : Int
{
    public UnsignedInt(LLVMCompiler compiler, string name, LLVMTypeRef llvmType, uint bitlength)
        : base(compiler, name, llvmType, bitlength) { }

    protected override ImplicitConversionTable GenerateImplicitConversions()
    {
        var dict = new ImplicitConversionTable(_compiler);
        var create = (Int destType) =>
        {
            dict.Add(
                destType,
                (prev) =>
                {
                    LLVMValueRef newVal;

                    if (Bits == 1)
                    {
                        newVal = _compiler.Builder.BuildZExt(prev.LLVMValue, destType.LLVMType);
                    }
                    else
                    {
                        newVal = _compiler.Builder.BuildIntCast(prev.LLVMValue, destType.LLVMType);
                    }

                    return Value.Create(_compiler, destType, newVal);
                }
            );
        };

        if (_compiler.Int64.Bits > Bits)
        {
            create(_compiler.Int64);
        }

        if (_compiler.Int32.Bits > Bits)
        {
            create(_compiler.Int32);
        }

        if (_compiler.Int16.Bits > Bits)
        {
            create(_compiler.Int16);
        }

        if (_compiler.Int8.Bits > Bits)
        {
            create(_compiler.Int8);
        }

        if (_compiler.UInt64.Bits > Bits)
        {
            create(_compiler.UInt64);
        }

        if (_compiler.UInt32.Bits > Bits)
        {
            create(_compiler.UInt32);
        }

        if (_compiler.UInt16.Bits > Bits)
        {
            create(_compiler.UInt16);
        }

        if (_compiler.UInt8.Bits > Bits)
        {
            create(_compiler.UInt8);
        }

        return dict;
    }
}
