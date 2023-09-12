using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class DecrementVarNode : StatementNode
{
    public RefNode RefNode { get; }

    public DecrementVarNode(RefNode refNode)
    {
        RefNode = refNode;
    }
}
