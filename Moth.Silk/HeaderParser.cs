using Index = ClangSharp.Index;
using ClangSharp.Interop;
using Microsoft.VisualBasic.FileIO;
using Moth.AST;
using Moth.AST.Node;
using System.Text;

namespace Moth.Silk;

public unsafe class HeaderParser : IDisposable
{
    private Options _options { get; }
    private string _path { get; }
    private Index _index { get; } = Index.Create(false, false);
    private CXTranslationUnit _unit { get; }
    private List<TypeNode> _types { get; } = new List<TypeNode>();
    private List<FuncDefNode> _funcs { get; } = new List<FuncDefNode>();
    private List<GlobalVarNode> _globals { get; } = new List<GlobalVarNode>();
    private bool _readyToDispose { get; set; } = false;
    
    // temporaries for the callbacks to set
    private List<FieldDefNode> _structFields { get; set; } = new List<FieldDefNode>();
    private List<ParameterNode> _funcParameters { get; set; } = new List<ParameterNode>();
    
    public HeaderParser(Options options, string path)
    {
        _options = options;
        _path = path;
        _unit = CXTranslationUnit.Parse(_index.Handle,
            _path,
            new ReadOnlySpan<string>(),
            new ReadOnlySpan<CXUnsavedFile>(),
            CXTranslationUnit_Flags.CXTranslationUnit_None);
    }

    public void Dispose()
    {
        _index.Dispose();
        _unit.Dispose();
    }

    public ScriptAST Parse()
    {
        if (_readyToDispose)
        {
            throw new Exception("File has already been parsed, expected Dispose.");
        }
        
        CXCursor c = _unit.Cursor;
        Visit(c, TopLevel);

        var nmspace = new NamespaceNode(_options.TopNamespace);
        nmspace.Child = new NamespaceNode("interop");
        _readyToDispose = true;
        return new ScriptAST(nmspace, new List<NamespaceNode>(), _types, new List<TraitNode>(), _funcs, _globals, new List<ImplementNode>());
    }

    private unsafe CXChildVisitResult Visit(CXCursor c, CXCursorVisitor v)
    {
        return c.VisitChildren(v, new CXClientData(IntPtr.Zero));
    }
    
    private CXChildVisitResult TopLevel(CXCursor c, CXCursor parent, void* data)
    {
        var cKind = c.Kind;
        
        if (_options.Verbose)
            Console.WriteLine($"[TopLevel] Cursor '{c.DisplayName}' of kind '{c.KindSpelling}'");
            
        switch (cKind)
        {
            case CXCursorKind.CXCursor_StructDecl:
                var structName = c.DisplayName.ToString();
                _structFields = new List<FieldDefNode>();
                Visit(c, StructLevel);
                var structFields = _structFields;
                var scopeStatements = new List<StatementNode>();
                scopeStatements.AddRange(structFields);
                if (scopeStatements.Count == 0)
                    break;
                _types.Add(new TypeNode(structName, PrivacyType.Pub, new ScopeNode(scopeStatements), new List<AttributeNode>()));
                break;
            case CXCursorKind.CXCursor_VarDecl:
                var globalName = c.DisplayName.ToString();
                var globalType = TranslateTypeRef(c.Type);
                _globals.Add(new GlobalVarNode(globalName, globalType, PrivacyType.Pub, c.IsConstexpr, true, new List<AttributeNode>()));
                break;
            case CXCursorKind.CXCursor_FunctionDecl:
                var funcName = c.Name.ToString(); //TODO: is this the right name?
                var funcReturnType = TranslateTypeRef(c.ReturnType);
                _funcParameters = new List<ParameterNode>();
                Visit(c, FunctionLevel);
                var funcParameters = _funcParameters;
                _funcs.Add(new FuncDefNode(funcName,
                    PrivacyType.Pub,
                    funcReturnType,
                    funcParameters,
                    null,
                    c.IsVariadic,
                    true,
                    true,
                    new List<AttributeNode>()));
                break;
            default:
                break;
        }
            
        return CXChildVisitResult.CXChildVisit_Continue;
    }
    
    private CXChildVisitResult StructLevel(CXCursor c, CXCursor parent, void* data)
    {
        var cKind = c.Kind;
        
        if (_options.Verbose)
            Console.WriteLine($"[StructLevel] Cursor '{c.DisplayName}' of kind '{c.KindSpelling}'");
            
        switch (cKind)
        {
            case CXCursorKind.CXCursor_FieldDecl:
                var fieldName = c.DisplayName.ToString();
                var fieldType = TranslateTypeRef(c.Type);
                _structFields.Add(new FieldDefNode(fieldName, PrivacyType.Pub, fieldType, new List<AttributeNode>()));
                break;
            default:
                break;
        }
            
        return CXChildVisitResult.CXChildVisit_Continue;
    }
    
    private CXChildVisitResult FunctionLevel(CXCursor c, CXCursor parent, void* data)
    {
        var cKind = c.Kind;
        
        if (_options.Verbose)
            Console.WriteLine($"[FunctionLevel] Cursor '{c.DisplayName}' of kind '{c.KindSpelling}'");
            
        switch (cKind)
        {
            case CXCursorKind.CXCursor_ParmDecl:
                var parameterName = c.DisplayName.ToString();
                var parameterType = TranslateTypeRef(c.Type);

                if (parameterName == String.Empty)
                    parameterName = "_";
                
                _funcParameters.Add(new ParameterNode(parameterName, parameterType));
                break;
            default:
                break;
        }
            
        return CXChildVisitResult.CXChildVisit_Continue;
    }

    private TypeRefNode TranslateTypeRef(CXType t)
    {
        TypeRefNode result = null;
        string oldT = t.ToString();
        uint pointerDepth = 0;
        
        while (result == null)
        {
            switch (t.kind)
            {
                case CXTypeKind.CXType_Pointer:
                    pointerDepth++;
                    t = t.PointeeType;
                    break;
                case CXTypeKind.CXType_ConstantArray:
                    result = new ConstSizeArrayTypeRefNode(TranslateTypeRef(t.ArrayElementType), pointerDepth, false, t.ArraySize);
                    break;
                default:
                    string name = t.kind switch
                    {
                        CXTypeKind.CXType_Elaborated => $"{t.CanonicalType.ToString().Replace("struct ", "")}",
                        CXTypeKind.CXType_Void => "void",
                        CXTypeKind.CXType_Bool => "bool",
                        CXTypeKind.CXType_Char_U or CXTypeKind.CXType_UChar => "u8",
                        CXTypeKind.CXType_UShort => "u16",
                        CXTypeKind.CXType_UInt => "u32",
                        CXTypeKind.CXType_ULong => $"u{IntPtr.Size * 8}",
                        CXTypeKind.CXType_ULongLong => "u64",
                        CXTypeKind.CXType_Char_S or CXTypeKind.CXType_SChar => "i8",
                        CXTypeKind.CXType_Short => "i16",
                        CXTypeKind.CXType_Int => "i32",
                        CXTypeKind.CXType_Long => $"i{IntPtr.Size * 8}",
                        CXTypeKind.CXType_LongLong => "i64",
                        CXTypeKind.CXType_Half or CXTypeKind.CXType_Float16 => "f16",
                        CXTypeKind.CXType_Float => "f32",
                        CXTypeKind.CXType_Double => "f64",
                        _ => throw new NotImplementedException($"Type kind \"{t.kind}\" is unsupported.")
                    };

                    result = new TypeRefNode(name, pointerDepth, false);
                    break;
            }
        }
        
        if (_options.Verbose)
            Console.WriteLine($"{oldT} becomes {result.GetSource()}");
        
        return result;
    }
}
