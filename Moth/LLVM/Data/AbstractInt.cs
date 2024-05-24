using Moth.AST.Node;
using System.Diagnostics.CodeAnalysis;

namespace Moth.LLVM.Data;

public class AbstractInt : PrimitiveStructDecl
{
    private ImplicitConversionTable _internalImplicits = null;
    private long _value;
    
    public AbstractInt(long value) : base("__abstract_integer", LLVMTypeRef.Int32, 32)
    {
        _value = value;
    }
    
    protected override Dictionary<string, OverloadList> GenerateDefaultMethods()
    {
        var dict = new Dictionary<string, OverloadList>();

        var addition = InitOperatorList(dict, OperationType.Addition);
        addition.Add(new AbstractIntOperation(this, OperationType.Addition));

        var subtraction = InitOperatorList(dict, OperationType.Subtraction);
        subtraction.Add(new AbstractIntOperation(this, OperationType.Subtraction));

        var multiplication = InitOperatorList(dict, OperationType.Multiplication);
        multiplication.Add(new AbstractIntOperation(this, OperationType.Multiplication));

        var division = InitOperatorList(dict, OperationType.Division);
        division.Add(new AbstractIntOperation(this, OperationType.Division));

        var exponential = InitOperatorList(dict, OperationType.Exponential);
        exponential.Add(new AbstractIntOperation(this, OperationType.Exponential));

        var modulus = InitOperatorList(dict, OperationType.Modulus);
        modulus.Add(new AbstractIntOperation(this, OperationType.Modulus));

        var lesserThan = InitOperatorList(dict, OperationType.LesserThan);
        lesserThan.Add(new AbstractIntOperation(this, OperationType.LesserThan));

        var lesserThanOrEqual = InitOperatorList(dict, OperationType.LesserThanOrEqual);
        lesserThanOrEqual.Add(new AbstractIntOperation(this, OperationType.LesserThanOrEqual));

        var greaterThan = InitOperatorList(dict, OperationType.GreaterThan);
        greaterThan.Add(new AbstractIntOperation(this, OperationType.GreaterThan));

        var greaterThanOrEqual = InitOperatorList(dict, OperationType.GreaterThanOrEqual);
        greaterThanOrEqual.Add(new AbstractIntOperation(this, OperationType.GreaterThanOrEqual));

        var equal = InitOperatorList(dict, OperationType.Equal);
        equal.Add(new AbstractIntOperation(this, OperationType.Equal));

        return dict;
    }

    public static Value Create(long value)
    {
        return new LiteralIntValue(value);
    }

    public override ImplicitConversionTable GetImplicitConversions()
    {
        if (_internalImplicits == null)
        {
            _internalImplicits = new ImplicitConversionTable(_value);
        }
        
        return _internalImplicits;
    }

    public override bool Equals(object? obj) => obj is AbstractInt;

    public class ImplicitConversionTable : LLVM.ImplicitConversionTable
    {
        private long _value;
        
        public ImplicitConversionTable(long value)
        {
            _value = value;
        }
        
        public override bool Contains(Type key)
        {
            if (key is not Int or Float)
            {
                return false;
            }

            if (key is Float @float)
            {
                throw new NotImplementedException();
            }

            if (key is Int @int)
            {
                return CanBeIntOfType(@int is SignedInt, @int.Bits);
            }

            throw new NotImplementedException();
        }

        bool CanBeIntOfType(bool isSigned, uint lengthInBits)
        {
            switch (lengthInBits)
            {
                case 1:
                case 8:
                case 16:
                case 32:
                case 64:
                case 128:
                    break;
                default:
                    throw new Exception($"Internal error: Integer cannot be of size \"{lengthInBits}\".");
            }

            long lowBound;
            long highBound;
            
            if (isSigned)
            {
                (lowBound, highBound) = lengthInBits switch
                {
                    1 => throw new Exception($"Internal error: Signed integer cannot be of size \"{lengthInBits}\"."),
                    8 => (-128, 127),
                    16 => (-32768, 32767),
                    32 => (-2147483648, 2147483647),
                    64 => (-9223372036854775808, 9223372036854775807),
                    _ => throw new NotImplementedException()
                };
            }
            else
            {
                if (_value < 0) return false;

                lowBound = 0;

                highBound = lengthInBits switch
                {
                    1 => 1,
                    8 => 255,
                    16 => 65535,
                    32 => 4294967295,
                    64 => 9223372036854775807,
                    _ => throw new NotImplementedException()
                };
                
            }

            return _value >= lowBound && _value <= highBound;
        }
        
        public override bool TryGetValue(Type key, [MaybeNullWhen(false)] out Func<LLVMCompiler, Value, Value> value)
        {
            if (Contains(key))
            {
                if (key is Float @float)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    bool isSigned = key is SignedInt;

                    value = (compiler, prev) =>
                    {
                        return Value.Create(key, LLVMValueRef.CreateConstInt(key.LLVMType, (ulong)_value, isSigned));
                    };
                }
                
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }
    }

    public class LiteralIntValue : Value
    {
        public override AbstractInt Type { get; }
        public long Value { get; }

        public override LLVMValueRef LLVMValue
        {
            get
            {
                return LLVMValueRef.CreateConstInt(Type.LLVMType, (ulong)Value, Type is SignedInt);
            }
        }
        
        public LiteralIntValue(long value) : base(null, null)
        {
            Type = new AbstractInt(value);
            Value = value;
        }
    }

    public class AbstractIntOperation : IntrinsicFunction
    {
        private AbstractInt _abstractIntType;
        private OperationType _opType;
        
        public AbstractIntOperation(AbstractInt abstractIntType, OperationType opType)
            : base(Utils.ExpandOpName(Utils.OpTypeToString(opType)),
                new FuncType(opType is OperationType.LesserThan or OperationType.LesserThanOrEqual
                        or OperationType.GreaterThan or OperationType.GreaterThanOrEqual or OperationType.Equal
                        ? Primitives.Bool
                        : abstractIntType,
                    new Type[]
                    {
                        new PtrType(abstractIntType),
                        abstractIntType
                    },
                    false))
        {
            _abstractIntType = abstractIntType;
            _opType = opType;
        }

        public override Value Call(LLVMCompiler compiler, Value[] args)
        {
            if (args.Length != 2) throw new Exception($"Internal error: Operation must have exactly two operands. {args.Length} were provided.");
            if (args[0] is not Pointer ptr || ptr.Type.BaseType is not AbstractInt) throw new Exception($"Internal error: Left operand is not a literal int.");
            if (args[1] is not LiteralIntValue rightVal) throw new Exception($"Internal error: Right operand is not a literal int.");

            var leftVal = new LiteralIntValue(_abstractIntType._value);
            object result = _opType switch
            {
                OperationType.Addition => leftVal.Value + rightVal.Value,
                OperationType.Subtraction => leftVal.Value - rightVal.Value,
                OperationType.Multiplication => leftVal.Value * rightVal.Value,
                OperationType.Division => leftVal.Value / rightVal.Value,
                OperationType.Exponential => Math.Pow(leftVal.Value, rightVal.Value),
                OperationType.Modulus => leftVal.Value % rightVal.Value,
                OperationType.LesserThan => leftVal.Value < rightVal.Value,
                OperationType.LesserThanOrEqual => leftVal.Value <= rightVal.Value,
                OperationType.GreaterThan => leftVal.Value > rightVal.Value,
                OperationType.GreaterThanOrEqual => leftVal.Value >= rightVal.Value,
                OperationType.Equal => leftVal.Value == rightVal.Value,
                _ => throw new NotImplementedException()
            };
            
            return result is bool b
                ? Value.Create(Primitives.Bool, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, (ulong)(b ? 1 : 0)))
                : AbstractInt.Create((long)result);
        }
    }
}
