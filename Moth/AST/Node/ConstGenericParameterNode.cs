using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.AST.Node;

public class ConstGenericParameterNode : GenericParameterNode
{
    public TypeRefNode TypeRef { get; set; }

    public ConstGenericParameterNode(string name, TypeRefNode typeRef) : base(name)
    {
        TypeRef = typeRef;
    }
}
