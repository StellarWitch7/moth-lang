using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class InferredLocalDefNode : LocalDefNode
{
    public InferredLocalDefNode(string name, ExpressionNode defaultVal) : base(name, null)
    {
        DefaultValue = defaultVal;
    }
}
