using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class AssignNamespaceNode : ASTNode
{
    public NamespaceNode NamespaceNode { get; }

    public AssignNamespaceNode(NamespaceNode namespaceNode)
    {
        NamespaceNode = namespaceNode;
    }
}
