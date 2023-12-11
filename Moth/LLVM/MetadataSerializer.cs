using LLVMSharp;
using Moth.LLVM.Data;
using Moth.LLVM.Reflection;
using System.Runtime.InteropServices;

namespace Moth.LLVM;

public unsafe class MetadataSerializer
{
    private List<Reflection.Type> _types = new List<Reflection.Type>();
    private List<Reflection.Field> _fields = new List<Reflection.Field>();
    private List<Reflection.Function> _functions = new List<Reflection.Function>();
    private List<Reflection.Global> _globals = new List<Reflection.Global>();
    private List<Reflection.Parameter> _params = new List<Reflection.Parameter>();
    private List<string> _names = new List<string>();
    private Dictionary<Data.Type, ulong> _typeIndexes = new Dictionary<Data.Type, ulong>();
    private uint _position = 0;
    private uint _typeTablePosition = 0;
    private uint _fieldTablePosition = 0;
    private uint _functionTablePosition = 0;
    private uint _globalTablePosition = 0;
    private uint _paramTablePosition = 0;
    private uint _nameTablePosition = 0;
    private LLVMCompiler _compiler;

    public MetadataSerializer(LLVMCompiler compiler)
    {
        _compiler = compiler;
    }
    
    public MemoryStream Process()
    {
        MemoryStream bytes = new MemoryStream();
        Reflection.Header header = new Reflection.Header();
        _position += (uint)sizeof(Reflection.Header);

        header.type_table_offset = _position;

        foreach (var @struct in _compiler.Types)
        {
            var type = new Reflection.Type();
            type.privacy = @struct.Privacy;
            type.is_struct = @struct is not Class;
            type.name_table_index = _nameTablePosition;
            type.name_table_length = (ulong)@struct.Name.Length;
            AddName(@struct.FullName);
            AddType(@struct, type);
        }

        header.field_table_offset = _position;
        
        foreach (var kv in _typeIndexes)
        {
            if (kv.Key is Struct @struct)
            {
                var type = (Reflection.Type)_types[(int)kv.Value];
                type.field_table_index = _fieldTablePosition;
                type.field_table_length = (uint)@struct.Fields.Count;

                foreach (var field in @struct.Fields.Values)
                {
                    var newField = new Reflection.Field();
                    newField.type_table_index = _typeIndexes[field.Type];
                    newField.privacy = field.Privacy;
                    newField.name_table_index = _nameTablePosition;
                    newField.name_table_length = (uint)field.Name.Length;
                    AddName(field.Name);
                    AddField(newField);
                }
            }
        }

        header.function_table_offset = _position;

        foreach (var func in _compiler.Functions)
        {
            var newFunc = new Reflection.Function();
            newFunc.type_table_index = _typeIndexes[func.ReturnType];
            newFunc.privacy = func.Privacy;
            newFunc.is_variadic = func.IsVariadic;
            newFunc.name_table_index = _nameTablePosition;
            newFunc.name_table_length = (uint)func.Name.Length; //TODO: is the name broken still?
            AddName(func.Name);

            foreach (var param in func.Params)
            {
                var newParam = new Reflection.Parameter();
                newParam.type_table_index = _typeIndexes[func.ParameterTypes[param.ParamIndex]];
                newParam.name_table_index = _nameTablePosition;
                newParam.name_table_length = (uint)param.Name.Length;
                AddName(param.Name);
            }
            
            AddFunction(newFunc);
        }
        
        // header.method_table_offset = _position;
        // header.static_method_table_offset = _position;
        
        header.global_variable_table_offset = _position;

        foreach (var global in _compiler.Globals)
        {
            var newGlobal = new Reflection.Global();
            newGlobal.privacy = global.Privacy;
            newGlobal.type_table_index = _typeIndexes[global.Type];
            newGlobal.name_table_index = _nameTablePosition;
            newGlobal.name_table_length = (uint)global.Name.Length;
            AddName(global.Name);
            AddGlobal(newGlobal);
        }
        
        header.param_table_offset = _position;
        header.name_table_offset = header.param_table_offset + _paramTablePosition;
        
        // write the result
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

        fixed (Reflection.Parameter* ptr = CollectionsMarshal.AsSpan(_params))
        {
            bytes.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Reflection.Parameter) * _params.Count));
        }

        foreach (var name in _names)
        {
            bytes.Write(System.Text.Encoding.UTF8.GetBytes(name));
        }

        return bytes;
    }
    
    public void AddType(Struct @struct, Reflection.Type type)
    {
        _typeIndexes.Add(@struct, _typeTablePosition);
        _types.Add(type);
        _position += (uint)sizeof(Reflection.Type);
        _typeTablePosition++;
    }

    public void AddField(Reflection.Field field)
    {
        _fields.Add(field);
        _position += (uint)sizeof(Reflection.Field);
        _fieldTablePosition++;
    }

    public void AddFunction(Reflection.Function func)
    {
        _functions.Add(func);
        _position += (uint)sizeof(Reflection.Function);
        _functionTablePosition++;
    }

    public void AddGlobal(Reflection.Global global)
    {
        _globals.Add(global);
        _position += (uint)sizeof(Reflection.Global);
        _globalTablePosition++;
    }

    public void AddParam(Reflection.Parameter param)
    {
        _params.Add(param);
        _position += (uint)sizeof(Reflection.Parameter);
        _paramTablePosition++;
    }

    public void AddName(string name)
    {
        _names.Add(name);
        _position += (uint)(sizeof(char) * name.Length);
        _nameTablePosition += (uint)name.Length;
    }
}
