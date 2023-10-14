using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class ParameterNode : ASTNode
{
    public string Name { get; set; }
    public TypeRefNode TypeRef { get; set; }

    public ParameterNode(string name, TypeRefNode typeRef)
    {
        Name = name;
        TypeRef = typeRef;
    }
}