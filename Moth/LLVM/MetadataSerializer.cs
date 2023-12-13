using LLVMSharp;
using Moth.LLVM.Data;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Moth.LLVM;

public unsafe class MetadataSerializer
{
    private List<Reflection.Type> _types = new List<Reflection.Type>();
    private List<Reflection.Field> _fields = new List<Reflection.Field>();
    private List<Reflection.Function> _functions = new List<Reflection.Function>();
    private List<Reflection.Function> _methods = new List<Reflection.Function>();
    private List<Reflection.Function> _staticMethods = new List<Reflection.Function>();
    private List<Reflection.Global> _globals = new List<Reflection.Global>();
    private List<Reflection.FuncType> _funcTypes = new List<Reflection.FuncType>();
    private List<Reflection.Parameter> _params = new List<Reflection.Parameter>();
    private List<Reflection.ParamType> _paramTypes = new List<Reflection.ParamType>();
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
    
    public MemoryStream Process()
    {
        MemoryStream bytes = new MemoryStream();
        Reflection.Header header = new Reflection.Header();

        foreach (var @struct in _compiler.Types)
        {
            var newType = new Reflection.Type();
            newType.privacy = @struct.Privacy;
            newType.is_struct = @struct is not Class;
            newType.name_table_index = _nameTablePosition;
            newType.name_table_length = (ulong)@struct.Name.Length;
            AddName(@struct.FullName);
            AddType(@struct, newType);
        }
        
        foreach (var kv in _typeIndexes)
        {
            if (kv.Key is Struct @struct)
            {
                var type = _types[(int)kv.Value];
                type.field_table_index = _fieldTablePosition;
                type.field_table_length = (uint)@struct.Fields.Count;

                foreach (var field in @struct.Fields.Values)
                {
                    var newField = new Reflection.Field();
                    newField.typeref_table_index = _typeRefTablePosition;
                    newField.typeref_table_length = AddTypeRef(header, field.Type);
                    newField.privacy = field.Privacy;
                    newField.name_table_index = _nameTablePosition;
                    newField.name_table_length = (uint)field.Name.Length;
                    AddName(field.Name);
                    AddField(newField);
                }
            }
        }

        foreach (var func in _compiler.Functions)
        {
            var newFunc = new Reflection.Function();
            newFunc.privacy = func.Privacy;
            newFunc.functype_table_index = AddTypeRef(header, func.Type);
            newFunc.name_table_index = _nameTablePosition;
            newFunc.name_table_length = (uint)func.Name.Length; //TODO: is the name broken still?
            AddName(func.Name);

            foreach (var param in func.Params)
            {
                var newParam = new Reflection.Parameter();
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
            var newGlobal = new Reflection.Global();
            newGlobal.privacy = global.Privacy;
            newGlobal.typeref_table_index = _typeRefTablePosition;
            newGlobal.typeref_table_length = AddTypeRef(header, global.Type);
            newGlobal.name_table_index = _nameTablePosition;
            newGlobal.name_table_length = (uint)global.Name.Length;
            AddName(global.Name);
            AddGlobal(newGlobal);
        }

        header.type_table_offset
            = (ulong)sizeof(Reflection.Header);
        header.field_table_offset
            = header.type_table_offset + (ulong)(sizeof(Reflection.Type) * _types.Count);
        header.function_table_offset
            = header.field_table_offset + (ulong)(sizeof(Reflection.Field) * _fields.Count);
        header.method_table_offset
            = header.function_table_offset + (ulong)(sizeof(Reflection.Function) * _functions.Count);
        header.static_method_table_offset
            = header.method_table_offset + (ulong)(sizeof(Reflection.Function) * _methods.Count);
        header.global_variable_table_offset
            = header.static_method_table_offset + (ulong)(sizeof(Reflection.Function) * _staticMethods.Count);
        header.functype_table_offset
            = header.global_variable_table_offset + (ulong)(sizeof(Reflection.Global) * _globals.Count);
        header.param_table_offset
            = header.functype_table_offset + (ulong)(sizeof(Reflection.FuncType) * _funcTypes.Count);
        header.paramtype_table_offset
            = header.param_table_offset + (ulong)(sizeof(Reflection.Parameter) * _params.Count);
        header.typeref_table_offset
            = header.paramtype_table_offset + (ulong)(sizeof(Reflection.ParamType) * _paramTypes.Count);
        header.name_table_offset
            = header.typeref_table_offset + (ulong)(sizeof(byte) * _typeRefs.Count);
        header.size
            = header.name_table_offset;

        foreach (var name in _names)
        {
            header.size += (ulong)(sizeof(char) * name.Length); // only works with ASCII
        }
        
        // write the result
        bytes.Write(System.Text.Encoding.UTF8.GetBytes("<metadata>"));
        bytes.Write(new ReadOnlySpan<byte>((byte*) &header, sizeof(Reflection.Header)));

        fixed (Reflection.Type* ptr = CollectionsMarshal.AsSpan(_types))
        {
            bytes.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Reflection.Type) * _types.Count));
        }

        fixed (Reflection.Field* ptr = CollectionsMarshal.AsSpan(_fields))
        {
            bytes.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Reflection.Field) * _fields.Count));
        }

        fixed (Reflection.Function* ptr = CollectionsMarshal.AsSpan(_functions))
        {
            bytes.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Reflection.Field) * _functions.Count));
        }

        fixed (Reflection.Global* ptr = CollectionsMarshal.AsSpan(_globals))
        {
            bytes.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Reflection.Global) * _globals.Count));
        }

        fixed (Reflection.FuncType* ptr = CollectionsMarshal.AsSpan(_funcTypes))
        {
            bytes.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Reflection.FuncType) * _funcTypes.Count));
        }

        fixed (Reflection.Parameter* ptr = CollectionsMarshal.AsSpan(_params))
        {
            bytes.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Reflection.Parameter) * _params.Count));
        }

        fixed (Reflection.ParamType* ptr = CollectionsMarshal.AsSpan(_paramTypes))
        {
            bytes.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Reflection.ParamType) * _paramTypes.Count));
        }

        fixed (byte* ptr = CollectionsMarshal.AsSpan(_typeRefs))
        {
            bytes.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(byte) * _typeRefs.Count));
        }

        foreach (var name in _names)
        {
            bytes.Write(System.Text.Encoding.UTF8.GetBytes(name));
        }
        
        bytes.Write(System.Text.Encoding.UTF8.GetBytes("</metadata>"));
        return bytes;
    }
    
    public void AddType(Struct @struct, Reflection.Type type)
    {
        _typeIndexes.Add(@struct, _typeTablePosition);
        _types.Add(type);
        _typeTablePosition++;
    }

    public void AddFuncType(Data.FuncType originalType, Reflection.FuncType type)
    {
        _functypeIndexes.Add(originalType, _functypeTablePosition);
        _funcTypes.Add(type);
        _functypeTablePosition++;
    }

    public void AddField(Reflection.Field field)
    {
        _fields.Add(field);
        _fieldTablePosition++;
    }

    public void AddFunction(Reflection.Function func)
    {
        _functions.Add(func);
        _functionTablePosition++;
    }

    public void AddGlobal(Reflection.Global global)
    {
        _globals.Add(global);
        _globalTablePosition++;
    }

    public void AddParam(Reflection.Parameter param)
    {
        _params.Add(param);
        _paramTablePosition++;
    }

    public void AddParamType(Reflection.ParamType paramType)
    {
        _paramTypes.Add(paramType);
        _paramTypeTablePosition++;
    }

    public void AddName(string name)
    {
        _names.Add(name);
        _nameTablePosition += (uint)name.Length;
    }
    
    public ulong AddTypeRef(Reflection.Header header, Type type)
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
                result.Add((byte)Reflection.TypeTag.Pointer);
                type = ptrType.BaseType;
            }
            else if (type == Primitives.Void)
            {
                result.Add((byte)Reflection.TypeTag.Void);
                type = null;
            }
            else if (type == Primitives.Bool)
            {
                result.Add((byte)Reflection.TypeTag.Bool);
                type = null;
            }
            else if (type == Primitives.Char || type == Primitives.UInt8)
            {
                result.Add((byte)Reflection.TypeTag.Char);
                type = null;
            }
            else if (type == Primitives.UInt16)
            {
                result.Add((byte)Reflection.TypeTag.UInt16);
                type = null;
            }
            else if (type == Primitives.UInt32)
            {
                result.Add((byte)Reflection.TypeTag.UInt32);
                type = null;
            }
            else if (type == Primitives.UInt64)
            {
                result.Add((byte)Reflection.TypeTag.UInt64);
                type = null;
            }
            else if (type == Primitives.Int8)
            {
                result.Add((byte)Reflection.TypeTag.Int8);
                type = null;
            }
            else if (type == Primitives.Int16)
            {
                result.Add((byte)Reflection.TypeTag.Int16);
                type = null;
            }
            else if (type == Primitives.Int32)
            {
                result.Add((byte)Reflection.TypeTag.Int32);
                type = null;
            }
            else if (type == Primitives.Int64)
            {
                result.Add((byte)Reflection.TypeTag.Int64);
                type = null;
            }
            else
            {
                if (_typeIndexes.TryGetValue(type, out ulong index))
                {
                    result.Add((byte)Reflection.TypeTag.Type);
                    result.AddRange(new ReadOnlySpan<byte>((byte*) &index, sizeof(ulong)).ToArray());
                    type = null;
                }
                else
                {
                    if (type is Data.FuncType fnType)
                    {
                        result.Add((byte)Reflection.TypeTag.FuncType);
                        
                        if (_functypeIndexes.TryGetValue(fnType, out index))
                        {
                            result.AddRange(new ReadOnlySpan<byte>((byte*) &index, sizeof(ulong)).ToArray());
                            type = null;
                        }
                        else
                        {
                            var newFuncType = new Reflection.FuncType();
                            newFuncType.is_variadic = fnType.IsVariadic;
                            newFuncType.return_typeref_table_index = _typeRefTablePosition;
                            newFuncType.return_typeref_table_length = AddTypeRef(header, fnType.ReturnType);
                            newFuncType.paramtype_table_index = _paramTypeTablePosition;
                            newFuncType.paramtype_table_length = (uint)fnType.ParameterTypes.Length;

                            foreach (var paramType in fnType.ParameterTypes)
                            {
                                var newParamType = new Reflection.ParamType();
                                newParamType.typeref_table_index = _typeRefTablePosition;
                                newParamType.typeref_table_length = AddTypeRef(header, paramType);
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
