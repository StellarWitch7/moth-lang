using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class ReturnNode : StatementNode
{
    public ExpressionNode ReturnValue { get; set; }

    public ReturnNode(ExpressionNode returnValue)
    {
        ReturnValue = returnValue;
    }
}
