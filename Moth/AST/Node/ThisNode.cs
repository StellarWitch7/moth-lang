using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class ThisNode : RefNode
{
    public ThisNode() : base(null)
    {
    }
}
