using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class IncrementVarNode : StatementNode
{
    public RefNode RefNode { get; }

    public IncrementVarNode(RefNode refNode)
    {
        RefNode = refNode;
    }
}
