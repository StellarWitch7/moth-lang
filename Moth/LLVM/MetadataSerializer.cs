using Moth.LLVM.Data;
using Moth.LLVM.Reflection;
using System.Runtime.InteropServices;

namespace Moth.LLVM;

public unsafe class MetadataSerializer
{
    private List<Reflection.Type> _types = new List<Reflection.Type>();
    private List<Reflection.Field> _fields = new List<Reflection.Field>();
    private List<string> _names = new List<string>();
    private Dictionary<Data.Type, ulong> _typeIndexes = new Dictionary<Data.Type, ulong>();
    private uint _position = 0;
    private uint _typeTablePosition = 0;
    private uint _fieldTablePosition = 0;
    private uint _nameTablePosition = 0;
    private LLVMCompiler _compiler;

    public MetadataSerializer(LLVMCompiler compiler)
    {
        _compiler = compiler;
    }
    
    public MemoryStream Process()
    {
        MemoryStream bytes = new MemoryStream();
        Header header = new Header();
        _position += (uint)sizeof(Header);

        header.type_table_offset = _position;

        foreach (var @struct in _compiler.Types)
        {
            var type = new Reflection.Type();
            type.privacy = @struct.Privacy;
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
                }
            }
        }

        header.name_table_offset = _position;
        
        // write the result
        bytes.Write(new ReadOnlySpan<byte>((byte*) &header, sizeof(Header)));

        fixed (Reflection.Type* ptr = CollectionsMarshal.AsSpan(_types))
        {
            bytes.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Reflection.Type) * _types.Count));
        }

        fixed (Reflection.Field* ptr = CollectionsMarshal.AsSpan(_fields))
        {
            bytes.Write(new ReadOnlySpan<byte>((byte*)ptr, sizeof(Reflection.Field) * _fields.Count));
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

    public void AddName(string name)
    {
        _names.Add(name);
        _position += (uint)(sizeof(char) * name.Length);
        _nameTablePosition += (uint)name.Length;
    }
}
