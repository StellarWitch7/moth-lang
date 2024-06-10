using ClangSharp;
using ClangSharp.Interop;
using Moth.LLVM;
using Moth.LLVM.Data;

namespace Moth;

public class HeaderBuilder
{
    public Dictionary<string, DefinedFunction> Functions { get; } =
        new Dictionary<string, DefinedFunction>();
    public Dictionary<string, StructDecl> Structs { get; } = new Dictionary<string, StructDecl>();
    public Dictionary<string, EnumDecl> Enums { get; } = new Dictionary<string, EnumDecl>();

    private LLVMCompiler _compiler { get; }
    private Index _index { get; } = Index.Create(false, false);
    private string _tmp
    {
        get => "libclang.ast.tmp";
    }
    private Language _lang { get; }

    public HeaderBuilder(LLVMCompiler compiler, Language lang)
    {
        _compiler = compiler;
        _lang = lang;
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

    public string Build()
    {
        _builder();
        return _out;
    }

    private unsafe void BuildCHeader()
    {
        throw new NotImplementedException(); //TODO: need to actually create the AST
        var unit = CXTranslationUnit.Create(_index.Handle, _tmp);
        unit.Save(_out, CXSaveTranslationUnit_Flags.CXSaveTranslationUnit_None);
    }

    private unsafe void BuildCPPHeader()
    {
        throw new NotImplementedException();
    }
}
