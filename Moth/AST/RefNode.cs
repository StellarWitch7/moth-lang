using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST;

public class RefNode : ExpressionNode
{
    public string Name { get; }

    public RefNode(string name)
    {
        Name = name;
    }
}
