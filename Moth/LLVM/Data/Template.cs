using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Template : CompilerData
{
    public Namespace Parent { get; }
    public string Name { get; }
    public PrivacyType Privacy { get; }
    public ScopeNode Contents { get; }

    public Namespace[] Imports { get; }
    public Dictionary<string, IAttribute> Attributes { get; }
    public TemplateParameter[] Params { get; }

    private Dictionary<string, Struct> _builtTypes = new Dictionary<string, Struct>();

    public Template(Namespace parent, string name, PrivacyType privacy, ScopeNode contents,
        Namespace[] imports, Dictionary<string, IAttribute> attributes, TemplateParameter[] @params)
    {
        Parent = parent;
        Name = name;
        Privacy = privacy;
        Contents = contents;
        Imports = imports;
        Attributes = attributes;
        Params = @params;
    }

    public Struct Build(LLVMCompiler compiler, IReadOnlyList<ExpressionNode> args)
    {
        string sig = ArgsToSig(args);
        
        if (args.Count != Params.Length)
        {
            throw new Exception($"Template arguments are {args.Count} long, expected {Params.Length} arguments for template \"{Name}\".");
        }

        if (_builtTypes.TryGetValue(sig, out Struct @struct))
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

        var classNode = new ClassNode($"{Name}{Template.ArgsToSig(args)}", Privacy, Contents, true);
        @struct = new Struct(Parent,
            classNode.Name,
            compiler.Context.CreateNamedStruct(classNode.Name),
            Privacy);
        _builtTypes.Add(sig, @struct);
        @compiler.BuildTemplate(this, classNode, @struct, args);
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
