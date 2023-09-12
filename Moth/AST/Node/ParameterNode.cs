using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class ParameterNode : ASTNode
{
    public DefinitionType Type { get; }
    public ClassRefNode? TypeRef { get; }
    public string Name { get; }

    public ParameterNode(DefinitionType type, string name, ClassRefNode typeRef)
    {
        if (type != DefinitionType.Void)
        {
            Type = type;
            Name = name;

            if (type == DefinitionType.ClassObject)
            {
                TypeRef = typeRef;
            }
            else
            {
                TypeRef = null;
            }
        }
        else
        {
            throw new ArgumentException();
        }
    }
}