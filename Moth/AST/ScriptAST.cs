using Moth.AST.Node;

namespace Moth.AST;

public class ScriptAST : IASTNode, ITreeNode
{
    public NamespaceNode Namespace { get; }
    public List<ITopDeclNode> Contents { get; }

    public ScriptAST(NamespaceNode @namespace, List<ITopDeclNode> contents)
    {
        Namespace = @namespace;
        Contents = contents;
    }

    public ScriptAST(
        NamespaceNode @namespace,
        List<ImportNode> imports,
        List<TypeNode> typeNodes,
        List<EnumNode> enumNodes,
        List<TraitNode> traitNodes,
        List<FuncDefNode> globalFuncs,
        List<GlobalVarNode> globalVariables,
        List<ImplementNode> implementNodes
    )
        : this(
            @namespace,
            Utils.Combine<ITopDeclNode>(
                imports,
                globalVariables,
                globalFuncs,
                typeNodes,
                enumNodes,
                traitNodes,
                implementNodes
            )
        ) { }

    public ImportNode[] Imports
    {
        get { return Contents.OfType<ImportNode>().ToArray(); }
    }
    public GlobalVarNode[] GlobalVariables
    {
        get { return Contents.OfType<GlobalVarNode>().ToArray(); }
    }

    public FuncDefNode[] GlobalFunctions
    {
        get { return Contents.OfType<FuncDefNode>().ToArray(); }
    }

    public TypeNode[] TypeNodes
    {
        get { return Contents.OfType<TypeNode>().ToArray(); }
    }

    public EnumNode[] EnumNodes
    {
        get { return Contents.OfType<EnumNode>().ToArray(); }
    }

    public TraitNode[] TraitNodes
    {
        get { return Contents.OfType<TraitNode>().ToArray(); }
    }

    public ImplementNode[] ImplementNodes
    {
        get { return Contents.OfType<ImplementNode>().ToArray(); }
    }

    public string GetSource()
    {
        var builder = new StringBuilder($"{Reserved.Namespace} {Namespace.GetSource()};\n");
        ITopDeclNode last = null;

        foreach (var statement in Contents)
        {
            if (statement is not ImportNode && last is ImportNode)
                builder.Append("\n");

            builder.Append($"\n{statement.GetSource()}");
            last = statement;
        }

        if (builder[builder.Length - 1] != '\n')
            builder.Append("\n");

        return builder.ToString();
    }

    public void PrintTree(TextWriter writer)
    {
        writer.Write(GetSource());
    }
}
