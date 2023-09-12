using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class IndexAccessNode : RefNode
{
    public ExpressionNode Index { get; }

    public IndexAccessNode(ExpressionNode index) : base(null)
    {
        Index = index;
    }
}
