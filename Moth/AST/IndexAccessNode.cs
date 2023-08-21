using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST;

public class IndexAccessNode : RefNode
{
    public RefNode Parent { get; }
    public ExpressionNode Index { get; }

    public IndexAccessNode(ExpressionNode index, RefNode parent) : base(null)
    {
        Index = index;
        Parent = parent;
    }
}
