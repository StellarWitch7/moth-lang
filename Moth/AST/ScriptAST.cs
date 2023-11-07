using Moth.AST.Node;

namespace Moth.AST;

public class ScriptAST : ASTNode
{
    public string Namespace { get; }
    public List<string> Imports { get; }
    public List<ClassNode> ClassNodes { get; }
    public List<FuncDefNode> GlobalFunctions { get; }
    public List<FieldDefNode> GlobalConstants { get; }

    public ScriptAST(string @namespace,
        List<string> imports,
        List<ClassNode> classNodes,
        List<FuncDefNode> globalFuncs,
        List<FieldDefNode> globalConstants)
    {
        Namespace = @namespace;
        Imports = imports;
        ClassNodes = classNodes;
        GlobalFunctions = globalFuncs;
        GlobalConstants = globalConstants;
    }
}
