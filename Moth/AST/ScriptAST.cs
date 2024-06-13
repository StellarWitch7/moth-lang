using Moth.AST.Node;
using Moth.LLVM;

namespace Moth.AST;

public class ScriptAST : IASTNode, ITreeNode
{
    public NamespaceNode Namespace { get; }
    public List<NamespaceNode> Imports { get; }
    public List<TypeNode> TypeNodes { get; }
    public List<EnumNode> EnumNodes { get; }
    public List<TraitNode> TraitNodes { get; }
    public List<FuncDefNode> GlobalFunctions { get; }
    public List<GlobalVarNode> GlobalVariables { get; }
    public List<ImplementNode> ImplementNodes { get; }

    public ScriptAST(
        NamespaceNode @namespace,
        List<NamespaceNode> imports,
        List<TypeNode> typeNodes,
        List<EnumNode> enumNodes,
        List<TraitNode> traitNodes,
        List<FuncDefNode> globalFuncs,
        List<GlobalVarNode> globalVariables,
        List<ImplementNode> implementNodes
    )
    {
        Namespace = @namespace;
        Imports = imports;
        TypeNodes = typeNodes;
        EnumNodes = enumNodes;
        TraitNodes = traitNodes;
        GlobalFunctions = globalFuncs;
        GlobalVariables = globalVariables;
        ImplementNodes = implementNodes;
    }

    public string GetSource()
    {
        var builder = new StringBuilder($"{Reserved.Namespace} {Namespace.GetSource()};\n");

        foreach (NamespaceNode import in Imports)
        {
            builder.Append($"\n{Reserved.With} {import.GetSource()};");
        }

        if (Imports.Count > 0)
            builder.Append("\n");

        f(builder, GlobalVariables);
        f(builder, GlobalFunctions);
        f(builder, TypeNodes);
        f(builder, EnumNodes);
        f(builder, TraitNodes);
        f(builder, ImplementNodes);
        return builder.ToString();
    }

    public void f<T>(StringBuilder builder, List<T> list)
        where T : IASTNode
    {
        foreach (T v in list)
        {
            builder.Append($"{v.GetSource()}");
        }

        if (list.Count > 0)
            builder.Append("\n");
    }

    public void PrintTree(TextWriter writer)
    {
        writer.Write(GetSource());
    }
}
