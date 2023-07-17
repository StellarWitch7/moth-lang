using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.Compiler.AST;

public class ImportNode : ASTNode
{
    public NamespaceNode NamespaceNode { get; }

    public ImportNode(NamespaceNode namespaceNode)
    {
        this.NamespaceNode = namespaceNode;
    }
}
