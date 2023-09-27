using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class ParameterNode : ASTNode
{
    public string Name { get; }
    public string TypeRef { get; }

    public ParameterNode(string name, string typeRef)
    {
        Name = name;
        TypeRef = typeRef;
    }
}