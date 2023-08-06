using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST;

public class ScopeNode : StatementNode
{
    public List<StatementNode> Statements { get; }

    public ScopeNode(List<StatementNode> statements)
    {
        Statements = statements;
    }
}
