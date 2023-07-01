using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser.AST;

public class IncrementVarNode : StatementNode
{
    public VariableRefNode VarRef { get; }

    public IncrementVarNode(VariableRefNode varRef)
    {
        VarRef = varRef;
    }
}
