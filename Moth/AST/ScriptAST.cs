using Moth.AST.Node;
using Moth.LLVM;

namespace Moth.AST;

public class ScriptAST : ASTNode
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

    public override string GetSource()
    {
        var builder = new StringBuilder($"{Reserved.Namespace} {Namespace.GetSource()};\n\n");

        foreach (NamespaceNode import in Imports)
        {
            builder.Append($"{Reserved.With} {import.GetSource()};\n");
        }

        if (Imports.Count > 0)
            builder.Append('\n');

        foreach (GlobalVarNode global in GlobalVariables)
        {
            builder.Append($"{global.GetSource()}\n");
        }

        if (GlobalVariables.Count > 0)
            builder.Append('\n');

        foreach (FuncDefNode func in GlobalFunctions)
        {
            builder.Append($"{func.GetSource()}\n\n");
        }

        foreach (TypeNode type in TypeNodes)
        {
            builder.Append($"{type.GetSource()}\n\n");
        }

        foreach (EnumNode @enum in EnumNodes)
        {
            builder.Append($"{@enum.GetSource()}\n\n");
        }

        foreach (TraitNode trait in TraitNodes)
        {
            builder.Append($"{trait.GetSource()}\n\n");
        }

        foreach (ImplementNode implement in ImplementNodes)
        {
            builder.Append($"{implement.GetSource()}\n\n");
        }

        return builder.ToString();
    }
}
