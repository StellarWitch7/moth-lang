using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class LocalDefNode : ExpressionNode
{
    public string Name { get; set; }
    public TypeRefNode TypeRef { get; set; }


    public LocalDefNode(string name, TypeRefNode typeRef)
    {
        Name = name;
        TypeRef = typeRef;
    }
}
