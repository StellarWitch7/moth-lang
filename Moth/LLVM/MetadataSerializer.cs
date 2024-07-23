using System.IO.Compression;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using LLVMSharp;
using Moth.LLVM.Data;

namespace Moth.LLVM;

public unsafe class MetadataSerializer : IDisposable
{
    private MemoryStream _stream = new MemoryStream();
    private List<Metadata.MetaType> _types = new List<Metadata.MetaType>();
    private List<Metadata.MetaField> _fields = new List<Metadata.MetaField>();
    private List<Metadata.MetaFunction> _functions = new List<Metadata.MetaFunction>();
    private List<Metadata.MetaGlobal> _globals = new List<Metadata.MetaGlobal>();
    private List<Metadata.MetaFuncType> _funcTypes = new List<Metadata.MetaFuncType>();
    private List<Metadata.MetaParameter> _params = new List<Metadata.MetaParameter>();
    private List<Metadata.MetaParamType> _paramTypes = new List<Metadata.MetaParamType>();
    private List<byte> _typeRefs = new List<byte>();
    private List<string> _names = new List<string>();
    private Dictionary<Type, uint> _typeIndexes = new Dictionary<Type, uint>();
    private Dictionary<FuncType, uint> _functypeIndexes = new Dictionary<FuncType, uint>();
    private uint _typeTablePosition;
    private uint _fieldTablePosition;
    private uint _functionTablePosition;
    private uint _globalTablePosition;
    private uint _functypeTablePosition;
    private uint _paramTablePosition;
    private uint _paramTypeTablePosition;
    private uint _nameTablePosition;
    private uint _typeRefTablePosition;
    private LLVMCompiler _compiler;

    public MetadataSerializer(LLVMCompiler compiler)
    {
        _compiler = compiler;
    }

    public void Dispose()
    {
        _stream.Dispose();
    }

    public MemoryStream Process()
    {
        var version = Meta.Version;
        var moduleVersion = _compiler.ModuleVersion;
        var header = new Metadata.MetaHeader();
        _compiler.Types.ForEach(AddType);
        _compiler.Functions.ForEach(AddFunction);
        _compiler.Globals.ForEach(AddGlobal);
        _stream.Seek(sizeof(Version) * 2 + sizeof(Metadata.MetaHeader), SeekOrigin.Current);

        OutListWithPos(&header.type_table_offset, _types);
        OutListWithPos(&header.field_table_offset, _fields);
        OutListWithPos(&header.function_table_offset, _functions);
        OutListWithPos(&header.global_variable_table_offset, _globals);
        OutListWithPos(&header.functype_table_offset, _funcTypes);
        OutListWithPos(&header.param_table_offset, _params);
        OutListWithPos(&header.paramtype_table_offset, _paramTypes);
        OutListWithPos(&header.typeref_table_offset, _typeRefs);

        header.name_table_offset = (uint)_stream.Position;

        foreach (var name in _names)
        {
            _stream.Write(System.Text.Encoding.UTF8.GetBytes(name));
        }

        header.size = (uint)_stream.Position;

        _stream.Seek(0, SeekOrigin.Begin);
        Out(&version);
        Out(&moduleVersion);
        Out(&header);
        _stream.Seek(0, SeekOrigin.End);
        return _stream;
    }

    public void AddType(TypeDecl typeDecl)
    {
        var newType = new Metadata.MetaType();
        newType.privacy = typeDecl.Privacy;
        newType.is_foreign = typeDecl is OpaqueStructDecl;
        newType.is_union = typeDecl.IsUnion;
        newType.name_table_index = _nameTablePosition;
        newType.name_table_length = (uint)typeDecl.FullName.Length;
        AddName(typeDecl.FullName);

        if (typeDecl is StructDecl structDecl && structDecl is not OpaqueStructDecl)
        {
            newType.field_table_index = _fieldTablePosition;
            newType.field_table_length = (uint)structDecl.Fields.Count;
            structDecl.Fields.Values.ToList().ForEach(AddField);
        }

        _typeIndexes.Add(typeDecl, _typeTablePosition);
        _types.Add(newType);
        _typeTablePosition++;
    }

    public uint AddFuncType(FuncType fnType)
    {
        var newFuncType = new Metadata.MetaFuncType();
        (uint retTyperefIndex, uint retTyperefLength) = AddTypeRef(fnType.ReturnType);

        newFuncType.is_variadic = fnType.IsVariadic;
        newFuncType.return_typeref_table_index = retTyperefIndex;
        newFuncType.return_typeref_table_length = retTyperefLength;
        newFuncType.paramtype_table_index = _paramTypeTablePosition;
        newFuncType.paramtype_table_length = (uint)fnType.ParameterTypes.Length;

        foreach (var paramType in fnType.ParameterTypes)
        {
            var newParamType = new Metadata.MetaParamType();
            (uint typerefIndex, uint typerefLength) = AddTypeRef(paramType);
            newParamType.typeref_table_index = typerefIndex;
            newParamType.typeref_table_length = typerefLength;
            AddParamType(newParamType);
        }

        var pos = _functypeTablePosition;
        _functypeIndexes.Add(fnType, _functypeTablePosition);
        _funcTypes.Add(newFuncType);
        _functypeTablePosition++;
        return pos;
    }

    public void AddField(Field field)
    {
        var newField = new Metadata.MetaField();
        (uint typerefIndex, uint typerefLength) = AddTypeRef(field.Type);

        newField.privacy = field.Privacy;
        newField.typeref_table_index = typerefIndex;
        newField.typeref_table_length = typerefLength;
        newField.name_table_index = _nameTablePosition;
        newField.name_table_length = (uint)field.Name.Length;

        AddName(field.Name);
        _fields.Add(newField);
        _fieldTablePosition++;
    }

    public void AddFunction(DefinedFunction func)
    {
        var newFunc = new Metadata.MetaFunction();
        (uint typerefIndex, uint typerefLength) = AddTypeRef(func.Type);

        newFunc.is_method = !func.IsStatic;
        newFunc.privacy = func.Privacy;
        newFunc.typeref_table_index = typerefIndex;
        newFunc.typeref_table_length = typerefLength;
        newFunc.name_table_index = _nameTablePosition;
        newFunc.name_table_length = (uint)func.FullName.Length;

        AddName(func.FullName);
        func.Params.ToList().ForEach(AddParam);
        _functions.Add(newFunc);
        _functionTablePosition++;
    }

    public void AddGlobal(IGlobal global)
    {
        var newGlobal = new Metadata.MetaGlobal();
        (uint typerefIndex, uint typerefLength) = AddTypeRef(global.Type.BaseType);

        newGlobal.privacy = global.Privacy;
        newGlobal.is_constant = global is GlobalConstant;
        newGlobal.typeref_table_index = typerefIndex;
        newGlobal.typeref_table_length = typerefLength;
        newGlobal.name_table_index = _nameTablePosition;
        newGlobal.name_table_length = (uint)global.FullName.Length;

        AddName(global.FullName);
        _globals.Add(newGlobal);
        _globalTablePosition++;
    }

    public void AddParam(Parameter param)
    {
        var newParam = new Metadata.MetaParameter();
        newParam.name_table_index = _nameTablePosition;
        newParam.name_table_length = (uint)param.Name.Length;
        newParam.param_index = param.ParamIndex;
        AddName(param.Name);

        _params.Add(newParam);
        _paramTablePosition++;
    }

    public void AddParamType(Metadata.MetaParamType metaParamType)
    {
        _paramTypes.Add(metaParamType);
        _paramTypeTablePosition++;
    }

    public void AddName(string name)
    {
        _names.Add(name);
        _nameTablePosition += (uint)name.Length;
    }

    public (uint, uint) AddTypeRef(Type type)
    {
        var result = new List<byte>();

        while (type != null)
        {
            if (type is RefType refType && type.GetType() == typeof(RefType))
            {
                result.Add((byte)Metadata.TypeTag.Reference);
                type = refType.BaseType;
            }
            if (type is PtrType ptrType && type.GetType() == typeof(PtrType))
            {
                result.Add((byte)Metadata.TypeTag.Pointer);
                type = ptrType.BaseType;
            }
            else if (type == _compiler.Void)
            {
                result.Add((byte)Metadata.TypeTag.Void);
                type = null;
            }
            else if (type == _compiler.Bool)
            {
                result.Add((byte)Metadata.TypeTag.Bool);
                type = null;
            }
            else if (type == _compiler.UInt8)
            {
                result.Add((byte)Metadata.TypeTag.UInt8);
                type = null;
            }
            else if (type == _compiler.UInt16)
            {
                result.Add((byte)Metadata.TypeTag.UInt16);
                type = null;
            }
            else if (type == _compiler.UInt32)
            {
                result.Add((byte)Metadata.TypeTag.UInt32);
                type = null;
            }
            else if (type == _compiler.UInt64)
            {
                result.Add((byte)Metadata.TypeTag.UInt64);
                type = null;
            }
            else if (type == _compiler.Int8)
            {
                result.Add((byte)Metadata.TypeTag.Int8);
                type = null;
            }
            else if (type == _compiler.Int16)
            {
                result.Add((byte)Metadata.TypeTag.Int16);
                type = null;
            }
            else if (type == _compiler.Int32)
            {
                result.Add((byte)Metadata.TypeTag.Int32);
                type = null;
            }
            else if (type == _compiler.Int64)
            {
                result.Add((byte)Metadata.TypeTag.Int64);
                type = null;
            }
            else
            {
                if (_typeIndexes.TryGetValue(type, out uint index))
                {
                    result.Add((byte)Metadata.TypeTag.Type);
                    result.AddRange(new ReadOnlySpan<byte>((byte*)&index, sizeof(uint)).ToArray());
                    type = null;
                }
                else
                {
                    if (type is FuncType fnType)
                    {
                        result.Add((byte)Metadata.TypeTag.FuncType);

                        if (!_functypeIndexes.TryGetValue(fnType, out index))
                            index = AddFuncType(fnType);

                        result.AddRange(VarSpan(&index).ToArray());
                        type = null;
                    }
                    else
                    {
                        throw new Exception(
                            $"Cannot retrieve non-indexed type, and cannot create it. "
                                + $"This is a CRITICAL ERROR. Report ASAP."
                        );
                    }
                }
            }
        }

        var tableIndex = _typeRefTablePosition;
        _typeRefs.AddRange(result);
        _typeRefTablePosition += (uint)result.Count;
        return (tableIndex, (uint)result.Count);
    }

    private ReadOnlySpan<byte> VarSpan<T>(T* var, int count = 1)
    {
        return new ReadOnlySpan<byte>((byte*)var, sizeof(T) * count);
    }

    private void Out<T>(T* var, int count = 1)
    {
        _stream.Write(VarSpan(var, count));
    }

    private void OutListWithPos<T>(uint* var, List<T> items)
    {
        (*var) = (uint)_stream.Position;

        fixed (T* ptr = CollectionsMarshal.AsSpan(items))
            Out(ptr, items.Count);
    }
}
