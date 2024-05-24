using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Template : ICompilerData
{
    public bool IsExternal { get; init; }
    public Namespace Parent { get; }
    public string Name { get; }
    public PrivacyType Privacy { get; }
    public bool IsUnion { get; }
    public ScopeNode Contents { get; }
    public Namespace[] Imports { get; }
    public Dictionary<string, IAttribute> Attributes { get; } = new Dictionary<string, IAttribute>();
    public TemplateParameter[] Params { get; }

    private LLVMCompiler _compiler;
    private List<AttributeNode> _attributeList;
    private Dictionary<string, StructDecl> _builtTypes = new Dictionary<string, StructDecl>();

    public Template(LLVMCompiler compiler, Namespace parent, string name, PrivacyType privacy, bool isUnion, ScopeNode contents,
        Namespace[] imports, List<AttributeNode> attributes, TemplateParameter[] @params)
    {
        _compiler = compiler;
        _attributeList = attributes;
        Parent = parent;
        Name = name;
        Privacy = privacy;
        IsUnion = isUnion;
        Contents = contents;
        Imports = imports;
        Params = @params;
        
        foreach (AttributeNode attribute in attributes)
        {
            Attributes.Add(attribute.Name, _compiler.MakeAttribute(attribute.Name, LLVMCompiler.CleanAttributeArgs(attribute.Arguments.ToArray())));
        }
    }

    public StructDecl Build(IReadOnlyList<ExpressionNode> args)
    {
        string sig = ArgsToSig(args);
        
        if (args.Count != Params.Length)
        {
            throw new Exception($"Template arguments are {args.Count} long, expected {Params.Length} arguments for template \"{Name}\".");
        }

        if (_builtTypes.TryGetValue(sig, out StructDecl @struct))
        {
            return @struct;
        }
        
        for (var i = 0; i < Params.Length; i++)
        {
            var param = Params[i];
            var arg = args[i];

            if (param.IsConst)
            {
                if (arg is not LiteralNode literal)
                {
                    throw new Exception($"Template argument {i} for template \"{Name}\" is expected to be a constant value.");
                }

                throw new NotImplementedException();
            }
            else
            {
                if (arg is not TypeRefNode typeRef)
                {
                    throw new Exception($"Template argument {i} for template \"{Name}\" is expected to be a type.");
                }
            }
        }

        var structNode = new TypeNode($"{Name}{Template.ArgsToSig(args)}", Privacy, Contents, IsUnion, _attributeList);
        @struct = new StructDecl(_compiler,
            Parent,
            structNode.Name,
            Privacy,
            IsUnion,
            Attributes,
            Contents);
        _builtTypes.Add(sig, @struct);
        _compiler.BuildTemplate(this, structNode, @struct, args);
        return @struct;
    }

    public static string ArgsToSig(IReadOnlyList<ExpressionNode> args)
    {
        var builder = new StringBuilder();

        builder.Append('<');

        foreach (var arg in args)
        {
            builder.Append(',');
        }

        builder.Append('>');
        return builder.ToString(); //TODO: improve
    }
}
