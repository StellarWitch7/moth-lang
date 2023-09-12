using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class DecrementVarNode : StatementNode
{
    public VariableRefNode VarRef { get; }

    public DecrementVarNode(VariableRefNode varRef)
    {
        VarRef = varRef;
    }
}
