using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class GenericParameterNode : ASTNode
{
    public string Name { get; set; }

    public GenericParameterNode(string name)
    {
        Name = name;
    }
}
