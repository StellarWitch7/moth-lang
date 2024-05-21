using Moth.AST.Node;

namespace Moth.AST;

public class ScriptAST : ASTNode
{
    public NamespaceNode Namespace { get; }
    public List<NamespaceNode> Imports { get; }
    public List<TypeNode> TypeNodes { get; }
    public List<TraitNode> TraitNodes { get; }
    public List<FuncDefNode> GlobalFunctions { get; }
    public List<FieldDefNode> GlobalVariables { get; }
    public List<ImplementNode> ImplementNodes { get; }

    public ScriptAST(NamespaceNode @namespace,
        List<NamespaceNode> imports,
        List<TypeNode> typeNodes,
        List<TraitNode> traitNodes,
        List<FuncDefNode> globalFuncs,
        List<FieldDefNode> globalVariables,
        List<ImplementNode> implementNodes)
    {
        Namespace = @namespace;
        Imports = imports;
        TypeNodes = typeNodes;
        TraitNodes = traitNodes;
        GlobalFunctions = globalFuncs;
        GlobalVariables = globalVariables;
        ImplementNodes = implementNodes;
    }
}
