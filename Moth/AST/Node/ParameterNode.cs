using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class ParameterNode : ASTNode
{
    public string Name { get; }
    public RefNode TypeRef { get; }

    public ParameterNode(string name, RefNode typeRef)
    {
        Name = name;
        TypeRef = typeRef;
    }
}