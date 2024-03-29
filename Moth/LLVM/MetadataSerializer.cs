using LLVMSharp;
using Moth.LLVM.Data;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Moth.LLVM;

public unsafe class MetadataSerializer
{
    private MemoryStream _stream = new MemoryStream();
    private List<Metadata.Type> _types = new List<Metadata.Type>();
    private List<Metadata.Field> _fields = new List<Metadata.Field>();
    private List<Metadata.Function> _functions = new List<Metadata.Function>();
    private List<Metadata.Function> _methods = new List<Metadata.Function>();
    private List<Metadata.Function> _staticMethods = new List<Metadata.Function>();
    private List<Metadata.Global> _globals = new List<Metadata.Global>();
    private List<Metadata.FuncType> _funcTypes = new List<Metadata.FuncType>();
    private List<Metadata.Parameter> _params = new List<Metadata.Parameter>();
    private List<Metadata.ParamType> _paramTypes = new List<Metadata.ParamType>();
    private List<byte> _typeRefs = new List<byte>();
    private List<string> _names = new List<string>();
    private Dictionary<Data.Type, ulong> _typeIndexes = new Dictionary<Data.Type, ulong>();
    private Dictionary<Data.FuncType, ulong> _functypeIndexes = new Dictionary<Data.FuncType, ulong>();
    private uint _typeTablePosition = 0;
    private uint _fieldTablePosition = 0;
    private uint _functionTablePosition = 0;
    private uint _globalTablePosition = 0;
    private uint _functypeTablePosition = 0;
    private uint _paramTablePosition = 0;
    private uint _paramTypeTablePosition = 0;
    private uint _nameTablePosition = 0;
    private uint _typeRefTablePosition = 0;
    private LLVMCompiler _compiler;

    public MetadataSerializer(LLVMCompiler compiler)
    {
        _compiler = compiler;
    }

    public MetadataSerializer(LLVMCompiler compiler, MemoryStream stream) : this(compiler)
    {
        _stream = stream;
    }

    public MemoryStream Process()
    {
        var startPos = (ulong)_stream.Position;
        
        foreach (var @struct in _compiler.Types)
        {
            var newType = new Metadata.Type();
            newType.privacy = @struct.Privacy;
            newType.is_foreign = @struct is OpaqueStruct;
            newType.field_table_index = _fieldTablePosition;
            newType.field_table_length = (uint)@struct.Fields.Count;
            newType.name_table_index = _nameTablePosition;
            newType.name_table_length = (ulong)@struct.FullName.Length;
            AddName(@struct.FullName);

            foreach (var field in @struct.Fields.Values)
            {
                var newField = new Metadata.Field();
                newField.privacy = field.Privacy;
                newField.typeref_table_index = _typeRefTablePosition;
                newField.typeref_table_length = AddTypeRef(field.Type);
                newField.name_table_index = _nameTablePosition;
                newField.name_table_length = (uint)field.Name.Length;
                AddName(field.Name);
                AddField(newField);
            }
            
            AddType(@struct, newType);
        }

        foreach (var func in _compiler.Functions)
        {
            var newFunc = new Metadata.Function();
            newFunc.privacy = func.Privacy;
            newFunc.functype_table_index = AddTypeRef(func.Type);
            newFunc.name_table_index = _nameTablePosition;
            newFunc.name_table_length = (uint)func.Name.Length; //TODO: is the name broken still?
            AddName(func.Name);

            foreach (var param in func.Params)
            {
                var newParam = new Metadata.Parameter();
                newParam.name_table_index = _nameTablePosition;
                newParam.name_table_length = (uint)param.Name.Length;
                newParam.param_index = param.ParamIndex;
                AddName(param.Name);
                AddParam(newParam);
            }
            
            AddFunction(newFunc);
        }

        foreach (var global in _compiler.Globals)
        {
            var newGlobal = new Metadata.Global();
            newGlobal.privacy = global.Privacy;
            newGlobal.typeref_table_index = _typeRefTablePosition;
            newGlobal.typeref_table_length = AddTypeRef(global.Type);
            newGlobal.name_table_index = _nameTablePosition;
            newGlobal.name_table_length = (uint)global.Name.Length;
            AddName(global.Name);
            AddGlobal(newGlobal);
        }
        
        // write the result
        var version = Meta.Version;
        _stream.Write(new ReadOnlySpan<byte>((byte*) &version, sizeof(Metadata.Version)));
        Metadata.Header header = new Metadata.Header();
        _stream.Write(new ReadOnlySpan<byte>((byte*) &header, sizeof(Metadata.Header)));

        header.type_table_offset = (ulong)_stream.Position - startPos;
        
        fixed (Metadata.Type* ptr = CollectionsMarshal.AsSpan(_types))
        {
            _stream.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Metadata.Type) * _types.Count));
        }

        header.field_table_offset = (ulong)_stream.Position - startPos;

        fixed (Metadata.Field* ptr = CollectionsMarshal.AsSpan(_fields))
        {
            _stream.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Metadata.Field) * _fields.Count));
        }

        header.function_table_offset = (ulong)_stream.Position - startPos;

        fixed (Metadata.Function* ptr = CollectionsMarshal.AsSpan(_functions))
        {
            _stream.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Metadata.Function) * _functions.Count));
        }

        header.method_table_offset = (ulong)_stream.Position - startPos;
        
        fixed (Metadata.Function* ptr = CollectionsMarshal.AsSpan(_methods))
        {
            _stream.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Metadata.Function) * _methods.Count));
        }

        header.static_method_table_offset = (ulong)_stream.Position - startPos;
        
        fixed (Metadata.Function* ptr = CollectionsMarshal.AsSpan(_staticMethods))
        {
            _stream.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Metadata.Function) * _staticMethods.Count));
        }

        header.global_variable_table_offset = (ulong)_stream.Position - startPos;

        fixed (Metadata.Global* ptr = CollectionsMarshal.AsSpan(_globals))
        {
            _stream.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Metadata.Global) * _globals.Count));
        }

        header.functype_table_offset = (ulong)_stream.Position - startPos;

        fixed (Metadata.FuncType* ptr = CollectionsMarshal.AsSpan(_funcTypes))
        {
            _stream.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Metadata.FuncType) * _funcTypes.Count));
        }

        header.param_table_offset = (ulong)_stream.Position - startPos;

        fixed (Metadata.Parameter* ptr = CollectionsMarshal.AsSpan(_params))
        {
            _stream.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Metadata.Parameter) * _params.Count));
        }

        header.paramtype_table_offset = (ulong)_stream.Position - startPos;

        fixed (Metadata.ParamType* ptr = CollectionsMarshal.AsSpan(_paramTypes))
        {
            _stream.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Metadata.ParamType) * _paramTypes.Count));
        }

        header.typeref_table_offset = (ulong)_stream.Position - startPos;

        fixed (byte* ptr = CollectionsMarshal.AsSpan(_typeRefs))
        {
            _stream.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(byte) * _typeRefs.Count));
        }

        header.name_table_offset = (ulong)_stream.Position - startPos;

        foreach (var name in _names)
        {
            _stream.Write(System.Text.Encoding.UTF8.GetBytes(name));
        }

        header.size = (ulong)_stream.Position - startPos;
        
        _stream.Position = (long)startPos;
        _stream.Write(new ReadOnlySpan<byte>((byte*) &version, sizeof(Metadata.Version)));
        _stream.Write(new ReadOnlySpan<byte>((byte*) &header, sizeof(Metadata.Header)));
        _stream.Position = _stream.Length;
        return _stream;
    }
    
    public void AddType(Struct @struct, Metadata.Type type)
    {
        _typeIndexes.Add(@struct, _typeTablePosition);
        _types.Add(type);
        _typeTablePosition++;
    }

    public void AddFuncType(Data.FuncType originalType, Metadata.FuncType type)
    {
        _functypeIndexes.Add(originalType, _functypeTablePosition);
        _funcTypes.Add(type);
        _functypeTablePosition++;
    }

    public void AddField(Metadata.Field field)
    {
        _fields.Add(field);
        _fieldTablePosition++;
    }

    public void AddFunction(Metadata.Function func)
    {
        _functions.Add(func);
        _functionTablePosition++;
    }

    public void AddGlobal(Metadata.Global global)
    {
        _globals.Add(global);
        _globalTablePosition++;
    }

    public void AddParam(Metadata.Parameter param)
    {
        _params.Add(param);
        _paramTablePosition++;
    }

    public void AddParamType(Metadata.ParamType paramType)
    {
        _paramTypes.Add(paramType);
        _paramTypeTablePosition++;
    }

    public void AddName(string name)
    {
        _names.Add(name);
        _nameTablePosition += (uint)name.Length;
    }
    
    public ulong AddTypeRef(Type type)
    {
        var result = new List<byte>();
        
        while (type != null)
        {
            if (type is RefType refType)
            {
                type = refType.BaseType;
            }
            if (type is PtrType ptrType && type.GetType() == typeof(PtrType))
            {
                result.Add((byte)Metadata.TypeTag.Pointer);
                type = ptrType.BaseType;
            }
            else if (type == Primitives.Void)
            {
                result.Add((byte)Metadata.TypeTag.Void);
                type = null;
            }
            else if (type == Primitives.Bool)
            {
                result.Add((byte)Metadata.TypeTag.Bool);
                type = null;
            }
            else if (type == Primitives.Char || type == Primitives.UInt8)
            {
                result.Add((byte)Metadata.TypeTag.Char);
                type = null;
            }
            else if (type == Primitives.UInt16)
            {
                result.Add((byte)Metadata.TypeTag.UInt16);
                type = null;
            }
            else if (type == Primitives.UInt32)
            {
                result.Add((byte)Metadata.TypeTag.UInt32);
                type = null;
            }
            else if (type == Primitives.UInt64)
            {
                result.Add((byte)Metadata.TypeTag.UInt64);
                type = null;
            }
            else if (type == Primitives.Int8)
            {
                result.Add((byte)Metadata.TypeTag.Int8);
                type = null;
            }
            else if (type == Primitives.Int16)
            {
                result.Add((byte)Metadata.TypeTag.Int16);
                type = null;
            }
            else if (type == Primitives.Int32)
            {
                result.Add((byte)Metadata.TypeTag.Int32);
                type = null;
            }
            else if (type == Primitives.Int64)
            {
                result.Add((byte)Metadata.TypeTag.Int64);
                type = null;
            }
            else
            {
                if (_typeIndexes.TryGetValue(type, out ulong index))
                {
                    result.Add((byte)Metadata.TypeTag.Type);
                    result.AddRange(new ReadOnlySpan<byte>((byte*) &index, sizeof(ulong)).ToArray());
                    type = null;
                }
                else
                {
                    if (type is Data.FuncType fnType)
                    {
                        result.Add((byte)Metadata.TypeTag.FuncType);
                        
                        if (_functypeIndexes.TryGetValue(fnType, out index))
                        {
                            result.AddRange(new ReadOnlySpan<byte>((byte*) &index, sizeof(ulong)).ToArray());
                            type = null;
                        }
                        else
                        {
                            var newFuncType = new Metadata.FuncType();
                            newFuncType.is_variadic = fnType.IsVariadic;
                            newFuncType.return_typeref_table_index = _typeRefTablePosition;
                            newFuncType.return_typeref_table_length = AddTypeRef(fnType.ReturnType);
                            newFuncType.paramtype_table_index = _paramTypeTablePosition;
                            newFuncType.paramtype_table_length = (uint)fnType.ParameterTypes.Length;

                            foreach (var paramType in fnType.ParameterTypes)
                            {
                                var newParamType = new Metadata.ParamType();
                                newParamType.typeref_table_index = _typeRefTablePosition;
                                newParamType.typeref_table_length = AddTypeRef(paramType);
                                AddParamType(newParamType);
                            }
                            
                            AddFuncType(fnType, newFuncType);
                        }
                    }
                    else
                    {
                        throw new Exception($"Cannot retrieve non-indexed type, and cannot create it. " +
                            $"This is a CRITICAL ERROR. Report ASAP.");
                    }
                }
            }
        }

        _typeRefs.AddRange(result);
        _typeRefTablePosition += (uint)result.Count;
        return (ulong)result.Count;
    }
}
