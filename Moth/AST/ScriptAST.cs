using Moth.AST.Node;

namespace Moth.AST;

public class ScriptAST : ASTNode
{
    public NamespaceNode Namespace { get; }
    public List<NamespaceNode> Imports { get; }
    public List<StructNode> ClassNodes { get; }
    public List<FuncDefNode> GlobalFunctions { get; }
    public List<FieldDefNode> GlobalVariables { get; }

    public ScriptAST(NamespaceNode @namespace,
        List<NamespaceNode> imports,
        List<StructNode> classNodes,
        List<FuncDefNode> globalFuncs,
        List<FieldDefNode> globalVariables)
    {
        Namespace = @namespace;
        Imports = imports;
        ClassNodes = classNodes;
        GlobalFunctions = globalFuncs;
        GlobalVariables = globalVariables;
    }
}
