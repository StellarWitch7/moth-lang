using LanguageParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.AST;

internal class ScriptAST : ASTNode
{
    public AssignNamespaceNode AssignNamespaceNode { get; }
    public List<ImportNode> ImportNodes { get; }
    public List<ClassNode> ClassNodes { get; }

    public ScriptAST(AssignNamespaceNode assignNamespaceNode,
        List<ImportNode> importNodes,
        List<ClassNode> classNodes)
    {
        this.AssignNamespaceNode = assignNamespaceNode;
        this.ImportNodes = importNodes;
        this.ClassNodes = classNodes;
    }
}
