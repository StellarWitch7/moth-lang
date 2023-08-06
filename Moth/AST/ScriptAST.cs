using Moth.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST;

public class ScriptAST : ASTNode
{
    public AssignNamespaceNode AssignNamespaceNode { get; }
    public List<ImportNode> ImportNodes { get; }
    public List<ClassNode> ClassNodes { get; }
    public List<MethodDefNode> GlobalFunctions { get; }

    public ScriptAST(AssignNamespaceNode assignNamespaceNode,
        List<ImportNode> importNodes,
        List<ClassNode> classNodes,
        List<MethodDefNode> globalFuncs)
    {
        this.AssignNamespaceNode = assignNamespaceNode;
        this.ImportNodes = importNodes;
        this.ClassNodes = classNodes;
        this.GlobalFunctions = globalFuncs;
    }
}
