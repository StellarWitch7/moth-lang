using System.Text;
using ClangSharp;
using ClangSharp.Interop;
using Moth.AST;
using Moth.AST.Node;

namespace Moth;

public unsafe class HeaderParser : IDisposable
{
    private bool _isVerbose { get; }
    private string _topNamespace { get; }
    private string _path { get; }
    private Index _index { get; } = Index.Create(false, false);
    private CXTranslationUnit _unit { get; }
    private Dictionary<string, NamespaceNode> _importsDict { get; } =
        new Dictionary<string, NamespaceNode>();
    private Dictionary<string, TypeNode> _typesDict { get; } = new Dictionary<string, TypeNode>();
    private Dictionary<string, EnumNode> _enumsDict { get; } = new Dictionary<string, EnumNode>();
    private List<NamespaceNode> _imports
    {
        get => _importsDict.Values.ToList();
    }
    private List<TypeNode> _types
    {
        get => _typesDict.Values.ToList();
    }
    private List<EnumNode> _enums
    {
        get => _enumsDict.Values.ToList();
    }
    private List<FuncDefNode> _funcs { get; } = new List<FuncDefNode>();
    private List<GlobalVarNode> _globals { get; } = new List<GlobalVarNode>();
    private bool _readyToDispose { get; set; } = false;

    // temporaries for the callbacks to set
    private List<FieldDefNode> _structFields { get; set; } = new List<FieldDefNode>();
    private List<ParameterNode> _funcParameters { get; set; } = new List<ParameterNode>();
    private List<EnumFlagNode> _enumFlags { get; set; } = new List<EnumFlagNode>();

    public HeaderParser(bool isVerbose, string topNamespace, string path)
    {
        _isVerbose = isVerbose;
        _topNamespace = topNamespace;
        _path = path;
        _unit = CXTranslationUnit.Parse(
            _index.Handle,
            _path,
            new ReadOnlySpan<string>(),
            new ReadOnlySpan<CXUnsavedFile>(),
            CXTranslationUnit_Flags.CXTranslationUnit_None
        );
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

        var baseNamespace = new NamespaceNode(_topNamespace);
        var interopNamespace = new NamespaceNode("interop");
        var libNamespace = new NamespaceNode(Path.GetFileNameWithoutExtension(_path));
        baseNamespace.Child = interopNamespace;
        interopNamespace.Child = libNamespace;
        _readyToDispose = true;
        return new ScriptAST(
            baseNamespace,
            _imports,
            _types,
            _enums,
            new List<TraitNode>(),
            _funcs,
            _globals,
            new List<ImplementNode>()
        );
    }

    private unsafe CXChildVisitResult Visit(CXCursor c, CXCursorVisitor v)
    {
        return c.VisitChildren(v, new CXClientData(IntPtr.Zero));
    }

    private CXChildVisitResult TopLevel(CXCursor c, CXCursor parent, void* data)
    {
        var cKind = c.Kind;

        if (_isVerbose)
            Console.WriteLine($"[TopLevel] Cursor '{c.DisplayName}' of kind '{c.KindSpelling}'");

        switch (cKind)
        {
            case CXCursorKind.CXCursor_UnionDecl:
            case CXCursorKind.CXCursor_StructDecl:
                var structName = c.DisplayName.ToString();
                if (structName.Contains("(unnamed"))
                    structName = ExtractAnonDeclName(structName);
                var isUnion = c.kind == CXCursorKind.CXCursor_UnionDecl;
                _structFields = new List<FieldDefNode>();
                Visit(c, StructLevel);
                var structFields = _structFields;
                var scopeStatements = new List<StatementNode>();
                scopeStatements.AddRange(structFields);
                var @struct = new TypeNode(
                    structName,
                    PrivacyType.Pub,
                    scopeStatements.Count == 0 ? null : new ScopeNode(scopeStatements),
                    isUnion,
                    new List<AttributeNode>()
                );
                _typesDict.TryAdd(structName, @struct);
                if (_typesDict[structName].Scope == null)
                    _typesDict[structName] = @struct;
                break;
            case CXCursorKind.CXCursor_EnumDecl:
                var enumName = c.DisplayName.ToString();
                if (enumName.Contains("(unnamed"))
                    enumName = ExtractAnonDeclName(enumName);
                _enumFlags = new List<EnumFlagNode>();
                Visit(c, EnumLevel);
                var enumFlags = _enumFlags;
                _enumsDict.TryAdd(
                    enumName,
                    new EnumNode(
                        enumName,
                        PrivacyType.Pub,
                        enumFlags,
                        null,
                        new List<AttributeNode>()
                    )
                );
                break;
            case CXCursorKind.CXCursor_VarDecl:
                var globalName = c.DisplayName.ToString();
                var globalType = TranslateTypeRef(c.Type);
                _globals.Add(
                    new GlobalVarNode(
                        globalName,
                        globalType,
                        PrivacyType.Pub,
                        c.IsConstexpr,
                        true,
                        new List<AttributeNode>()
                    )
                );
                break;
            case CXCursorKind.CXCursor_FunctionDecl:
                var funcName = c.Name.ToString();
                var funcReturnType = TranslateTypeRef(c.ReturnType);
                _funcParameters = new List<ParameterNode>();
                Visit(c, FunctionLevel);
                var funcParameters = _funcParameters;
                _funcs.Add(
                    new FuncDefNode(
                        funcName,
                        PrivacyType.Pub,
                        funcReturnType,
                        funcParameters,
                        null,
                        c.IsVariadic,
                        false,
                        true,
                        new List<AttributeNode>()
                    )
                );
                break;
            default:
                break;
        }

        return CXChildVisitResult.CXChildVisit_Continue;
    }

    private CXChildVisitResult StructLevel(CXCursor c, CXCursor parent, void* data)
    {
        var cKind = c.Kind;

        if (_isVerbose)
            Console.WriteLine($"[StructLevel] Cursor '{c.DisplayName}' of kind '{c.KindSpelling}'");

        switch (cKind)
        {
            case CXCursorKind.CXCursor_FieldDecl:
                var fieldName = c.DisplayName.ToString();
                var fieldType = TranslateTypeRef(c.Type);
                _structFields.Add(
                    new FieldDefNode(
                        fieldName,
                        PrivacyType.Pub,
                        fieldType,
                        new List<AttributeNode>()
                    )
                );
                break;
            default:
                break;
        }

        return CXChildVisitResult.CXChildVisit_Continue;
    }

    private CXChildVisitResult EnumLevel(CXCursor c, CXCursor parent, void* data)
    {
        var cKind = c.Kind;

        if (_isVerbose)
            Console.WriteLine($"[EnumLevel] Cursor '{c.DisplayName}' of kind '{c.KindSpelling}'");

        switch (cKind)
        {
            case CXCursorKind.CXCursor_EnumConstantDecl:
                var flagName = c.DisplayName.ToString();
                var flagValue = c.EnumConstantDeclUnsignedValue;
                if (flagValue > UInt64.MaxValue)
                    throw new Exception(
                        $"Cannot convert C enum declaration to Moth enum declaration, "
                            + $"enum value (\"{flagValue}\") for \"{flagName}\" is greater than 64 bits."
                    );
                _enumFlags.Add(new EnumFlagNode(flagName, flagValue));
                break;
            default:
                break;
        }

        return CXChildVisitResult.CXChildVisit_Continue;
    }

    private CXChildVisitResult FunctionLevel(CXCursor c, CXCursor parent, void* data)
    {
        var cKind = c.Kind;

        if (_isVerbose)
            Console.WriteLine(
                $"[FunctionLevel] Cursor '{c.DisplayName}' of kind '{c.KindSpelling}'"
            );

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
            t = t.CanonicalType;

            switch (t.kind)
            {
                case CXTypeKind.CXType_Pointer:
                    pointerDepth++;
                    t = t.PointeeType;
                    break;
                case CXTypeKind.CXType_FunctionProto:
                    var paramTypes = new List<TypeRefNode>();

                    for (uint i = 0; i < t.NumArgTypes; i++)
                    {
                        paramTypes.Add(TranslateTypeRef(t.GetArgType(i)));
                    }

                    result = new FuncTypeRefNode(
                        TranslateTypeRef(t.ResultType),
                        paramTypes,
                        pointerDepth,
                        false
                    );
                    break;
                case CXTypeKind.CXType_FunctionNoProto:
                    result = new FuncTypeRefNode(
                        TranslateTypeRef(t.ResultType),
                        new List<TypeRefNode>(),
                        pointerDepth,
                        false
                    );
                    break;
                case CXTypeKind.CXType_ConstantArray:
                    result = new ConstSizeArrayTypeRefNode(
                        TranslateTypeRef(t.ArrayElementType),
                        pointerDepth,
                        false,
                        t.ArraySize
                    );
                    break;
                case CXTypeKind.CXType_DependentSizedArray:
                case CXTypeKind.CXType_IncompleteArray:
                case CXTypeKind.CXType_VariableArray:
                case CXTypeKind.CXType_Vector:
                    result = TranslateTypeRef(t.ElementType);
                    result.PointerDepth++;
                    break;
                case CXTypeKind.CXType_Complex:
                    result = new TemplateTypeRefNode(
                        "Complex",
                        new List<ExpressionNode>() { TranslateTypeRef(t.ElementType) },
                        pointerDepth,
                        false
                    );
                    var coreMath = new NamespaceNode("core");
                    coreMath.Child = new NamespaceNode("math");
                    WithNamespace(coreMath);
                    break;
                default:
                    string name;

                    if (
                        t.kind == CXTypeKind.CXType_Elaborated
                        || t.kind == CXTypeKind.CXType_Record
                        || t.kind == CXTypeKind.CXType_Enum
                    )
                    {
                        name = t.ToString()
                            .Replace("struct ", "")
                            .Replace("const ", "")
                            .Replace("union ", "")
                            .Replace("enum ", "");

                        if (t.ToString().Contains("(unnamed at"))
                        {
                            Console.WriteLine(
                                $"Creating named definition for unnamed type \"{t}\"..."
                            );
                            var isStruct = t.Declaration.kind == CXCursorKind.CXCursor_StructDecl;
                            var isUnion = t.Declaration.kind == CXCursorKind.CXCursor_UnionDecl;
                            var isEnum = t.Declaration.kind == CXCursorKind.CXCursor_EnumDecl;
                            name = ExtractAnonDeclName(t);

                            if (isStruct || isUnion)
                            {
                                _structFields = new List<FieldDefNode>();
                                Visit(t.Declaration, StructLevel);
                                var structFields = _structFields;
                                var scopeStatements = new List<StatementNode>();
                                scopeStatements.AddRange(structFields);
                                _typesDict.TryAdd(
                                    name,
                                    new TypeNode(
                                        name,
                                        PrivacyType.Pub,
                                        new ScopeNode(scopeStatements),
                                        isUnion,
                                        new List<AttributeNode>()
                                    )
                                );
                            }
                            else if (isEnum)
                            {
                                _enumFlags = new List<EnumFlagNode>();
                                Visit(t.Declaration, EnumLevel);
                                var enumFlags = _enumFlags;
                                _enumsDict.TryAdd(
                                    name,
                                    new EnumNode(
                                        name,
                                        PrivacyType.Pub,
                                        enumFlags,
                                        null,
                                        new List<AttributeNode>()
                                    )
                                );
                            }
                            else
                            {
                                throw new Exception(
                                    $"Unnamed declaration \"{t.Declaration.kind}\" is unsupported."
                                );
                            }
                        }
                    }
                    else
                    {
                        name = t.kind switch
                        {
                            CXTypeKind.CXType_Void => "void",
                            CXTypeKind.CXType_Bool => "bool",
                            CXTypeKind.CXType_Char_U or CXTypeKind.CXType_UChar => "u8",
                            CXTypeKind.CXType_UShort => "u16",
                            CXTypeKind.CXType_UInt => $"u32",
                            CXTypeKind.CXType_ULong => $"u{IntPtr.Size * 8}",
                            CXTypeKind.CXType_ULongLong => "u64",
                            CXTypeKind.CXType_UInt128 => "u128",
                            CXTypeKind.CXType_Char_S or CXTypeKind.CXType_SChar => "i8",
                            CXTypeKind.CXType_Short => "i16",
                            CXTypeKind.CXType_Int => $"i32",
                            CXTypeKind.CXType_Long => $"i{IntPtr.Size * 8}",
                            CXTypeKind.CXType_LongLong => "i64",
                            CXTypeKind.CXType_Int128 => "i128",
                            CXTypeKind.CXType_Half or CXTypeKind.CXType_Float16 => "f16",
                            CXTypeKind.CXType_Float => "f32",
                            CXTypeKind.CXType_Double => $"f{IntPtr.Size * 8}", //TODO: the double and long double may be wrong
                            CXTypeKind.CXType_LongDouble => "f64",
                            _
                                => throw new NotImplementedException(
                                    $"Type kind \"{t.kind}\" (\"{t}\") is unsupported."
                                )
                        };
                    }

                    result = new TypeRefNode(name, pointerDepth, false);
                    break;
            }
        }

        if (_isVerbose)
            Console.WriteLine($"{oldT} becomes {result.GetSource()}");

        return result;
    }

    private void WithNamespace(NamespaceNode nmspace)
    {
        string key = nmspace.GetSource();
        _importsDict.TryAdd(key, nmspace);
    }

    private string ExtractAnonDeclName(CXType t)
    {
        return ExtractAnonDeclName(t.ToString());
    }

    private string ExtractAnonDeclName(string s)
    {
        s = s.Remove(0, s.IndexOf('/')).Replace(")", "");
        var sections = s.Split(':', StringSplitOptions.RemoveEmptyEntries);
        var headerName = Path.GetFileNameWithoutExtension(sections[0])
            .Replace(".", "_")
            .Replace("-", "_");
        var lineNum = sections[1];
        var colNum = sections[2];

        return $"anon_{headerName}_{lineNum}_{colNum}";
    }
}
