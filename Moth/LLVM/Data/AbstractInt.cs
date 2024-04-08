using System.Diagnostics.CodeAnalysis;

namespace Moth.LLVM.Data;

public class AbstractInt : Type
{
    private long _value;
    
    public AbstractInt(long value) : base(null, TypeKind.Struct)
    {
        _value = value;
    }

    public static Value Create(long value)
    {
        return new LiteralIntValue(value);
    }

    public override ImplicitConversionTable GetImplicitConversions() => new ImplicitConversionTable(_value);

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
        public override Int Type
        {
            get
            {
                if (_abstractType.CanConvertTo(Primitives.Int32)) return Primitives.Int32;
                else if (_abstractType.CanConvertTo(Primitives.Int64)) return Primitives.Int64;
                else throw new Exception("Internal error: Abstract int is too large.");
            }
        }

        public override LLVMValueRef LLVMValue
        {
            get
            {
                return LLVMValueRef.CreateConstInt(Type.LLVMType, (ulong)_value, Type is SignedInt);
            }
        }

        private AbstractInt _abstractType;
        private long _value;
        
        public LiteralIntValue(long value) : base(null, null)
        {
            _abstractType = new AbstractInt(value);
            _value = value;
        }

        public override Value ImplicitConvertTo(LLVMCompiler compiler, Type target)
        {
            var implicits = _abstractType.GetImplicitConversions();

            if (implicits.TryGetValue(target, out Func<LLVMCompiler, Value, Value> convert))
            {
                return convert(compiler, this);
            }
            else
            {
                throw new Exception($"Cannot implicitly convert value of type \"{Type}\" to \"{target}\".");
            }
        }
    }
}
