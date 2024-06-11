using Moth.LLVM;
using Moth.LLVM.Data;

namespace Moth;

public class HeaderBuilder
{
    public HashSet<DefinedFunction> Functions { get; } = new HashSet<DefinedFunction>();
    public HashSet<StructDecl> Structs { get; } = new HashSet<StructDecl>();
    public HashSet<EnumDecl> Enums { get; } = new HashSet<EnumDecl>();

    private LLVMCompiler _compiler { get; }
    private Index _index { get; } = Index.Create(false, false);
    private Language _lang { get; set; }
    private string _tmp
    {
        get => ".tmp";
    }

    public HeaderBuilder(LLVMCompiler compiler)
    {
        _compiler = compiler;
    }

    private Action _builder
    {
        get
        {
            return _lang switch
            {
                Language.C => BuildCHeader,
                Language.CPP => BuildCPPHeader,
                _ => throw new NotImplementedException()
            };
        }
    }

    private string _out
    {
        get
        {
            return _compiler.ModuleName
                + _lang switch
                {
                    Language.C => ".h",
                    Language.CPP => $".hh",
                    _ => throw new NotImplementedException()
                };
        }
    }

    public string Build(Language lang)
    {
        _lang = lang;
        _builder();
        return _out;
    }

    private void BuildCHeader()
    {
        using (var file = new StreamWriter(File.Create(_out)))
        {
            file.WriteLine("#include <stdint.h>");
            file.WriteLine("#include <stdbool.h>");
            file.WriteLine();

            foreach (var v in Structs)
            {
                file.WriteLine(SerializeDeclToC(v));
            }

            foreach (var v in Functions)
            {
                file.WriteLine(SerializeDeclToC(v));
            }
        }
    }

    private string SerializeDeclToC(object decl)
    {
        if (decl is DefinedFunction func)
        {
            var builder = new StringBuilder();

            foreach (var param in func.Params)
            {
                builder.Append(
                    $"{SerializeTypeToC(func.Type.ParameterTypes[param.ParamIndex], param.Name)}, "
                );
            }

            if (builder.Length > 0)
                builder.Remove(builder.Length - 2, 2);

            if (func.IsVariadic)
                builder.Append(", ...");

            return $"{SerializeTypeToC(func.Type.ReturnType, Utils.MakeStubName(Language.C, func.FullName))}({builder});\n";
        }

        if (decl is StructDecl structDecl)
        {
            string stubName = Utils.MakeStubName(Language.C, structDecl.FullName);
            var builder = new StringBuilder($"typedef struct {stubName}");

            if (structDecl is OpaqueStructDecl)
                builder.Append(";\n");
            else
            {
                builder.Append(" {\n");

                foreach (var field in structDecl.Fields.Values)
                {
                    builder.Append($"    {SerializeTypeToC(field.Type, field.Name)};\n");
                }

                builder.Append($"}} {stubName};\n");
            }

            return builder.ToString();
        }

        throw new NotImplementedException();
    }

    private string SerializeTypeToC(Type type, string declName = "")
    {
        if (type is FuncType funcType)
        {
            var builder = new StringBuilder();

            foreach (var paramType in funcType.ParameterTypes)
            {
                builder.Append($"{SerializeTypeToC(paramType)}, ");

                if (builder[builder.Length - 3] == ' ')
                    builder.Remove(builder.Length - 3, 1);
            }

            if (builder.Length > 0)
                builder.Remove(builder.Length - 2, 2);

            if (funcType.IsVariadic)
                builder.Append(", ...");

            if (declName == "")
            {
                return $"{SerializeTypeToC(funcType.ReturnType)}({builder}) ";
            }
            else
            {
                return $"{SerializeTypeToC(funcType.ReturnType)}(*{declName})({builder})";
            }
        }

        if (type is PtrType ptrType)
        {
            if (ptrType.BaseType is FuncType)
                return $"{SerializeTypeToC(ptrType.BaseType, $"*{declName}")}";

            return $"{SerializeTypeToC(ptrType.BaseType)}*{declName}";
        }

        if (type is PrimitiveStructDecl primitive)
        {
            if (primitive is Void)
                return $"void {declName}";

            if (primitive is Int i)
            {
                if (i.Bits == 1)
                    return $"bool";

                string signage = i is UnsignedInt ? "u" : "";
                return $"{signage}int{i.Bits}_t {declName}";
            }

            if (primitive is Float f)
            {
                string size = f.Bits switch
                {
                    64 => "double",
                    32 => "float",
                    _ => throw new Exception($"Illegal float size: {f.Bits}")
                };

                return $"{size} {declName}";
            }
        }

        if (type is StructDecl structDecl)
        {
            return $"{Utils.MakeStubName(Language.C, structDecl.FullName)} {declName}";
        }

        throw new NotImplementedException();
    }

    private void BuildCPPHeader()
    {
        throw new NotImplementedException();
    }
}
